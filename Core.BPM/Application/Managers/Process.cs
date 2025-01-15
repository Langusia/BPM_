using Core.BPM.Configuration;
using Core.BPM.Interfaces;
using Core.BPM.Nodes;
using Core.BPM.Persistence;
using MediatR;

namespace Core.BPM.Application.Managers;

public class Process : IProcess, IProcessStore
{
    private readonly IBpmRepository _repository;
    private readonly List<object> _storedEvents = [];
    private readonly Queue<object> _uncommittedEvents = [];
    private readonly Guid _aggregateId;
    private readonly string _aggregateName;
    private readonly BProcess _processConfig;
    private bool _isNewProcess;

    public Process(Guid aggregateId, string rootAggregateName, bool isNewProcess, IEnumerable<object>? events, IBpmRepository repository)
    {
        _processConfig = BProcessGraphConfiguration.GetConfig(rootAggregateName) ?? throw new Exception();
        if (events is not null)
            _storedEvents = events.ToList();

        _aggregateId = aggregateId;
        _aggregateName = rootAggregateName;
        _isNewProcess = isNewProcess;

        _repository = repository;
    }


    public T AggregateAs<T>(bool includeUncommitted = true) where T : Aggregate
    {
        var stream = _storedEvents;
        if (includeUncommitted)
            stream = stream.Union(_uncommittedEvents).ToList();

        var aggregate = (T)_repository.AggregateStreamFromRegistry(typeof(T), stream);
        aggregate.Id = _aggregateId;
        return aggregate;
    }

    public T AggregateOrDefaultAs<T>(bool includeUncommitted) where T : Aggregate
    {
        var stream = _storedEvents;
        if (includeUncommitted)
            stream = stream.Union(_uncommittedEvents).ToList();


        var aggregate = (T)_repository.AggregateOrDefaultStreamFromRegistry(typeof(T), stream);
        aggregate.Id = _aggregateId;
        return aggregate;
    }

    public T? AggregateOrNullAs<T>(bool includeUncommitted) where T : Aggregate
    {
        var stream = _storedEvents;
        if (includeUncommitted)
            stream = stream.Union(_uncommittedEvents).ToList();


        var aggregateObj = _repository.AggregateOrNullStreamFromRegistry(typeof(T), stream);
        if (aggregateObj is null)
            return null;

        var aggregate = (T)aggregateObj;
        aggregate.Id = _aggregateId;

        return aggregate;
    }

    public bool AppendEvents(params object[] events)
    {
        _uncommittedEvents.Enqueue(events);
        var filtered = UnlockedPaths();
        if (filtered.All(f => events.Select(e => e.GetType().Name).Any(z => !f.ProducingEvents.Select(c => c.Name).Contains(z))))
            return false;

        return true;
    }

    public bool Validate<T>(bool includeUncommitted) where T : IBaseRequest
    {
        var filtered = UnlockedPaths(includeUncommitted);
        return filtered.Any(x => x.CommandType == typeof(T));
    }

    public List<INode> GetNextSteps(bool includeUnsavedEvents = true)
    {
        return UnlockedPaths(includeUnsavedEvents);
    }


    private List<INode> UnlockedPaths(bool includeUnsavedEvents = true)
    {
        var stream = _storedEvents;
        if (includeUnsavedEvents)
            stream = stream.Union(_uncommittedEvents).ToList();

        // Step 1: Map events to corresponding nodes
        var mappedNodes = MapEventsToNodes(stream, _processConfig);

        // Step 2: Group nodes by CommandType or unique key
        var storedGroups = GroupNodesByType(mappedNodes);

        // Step 3: Filter possible paths based on stored groups
        var possiblePaths = FilterPossiblePaths(storedGroups);

        // Step 4: Extract next steps from filtered paths
        var filtered = ExtractNextSteps(possiblePaths, storedGroups);

        // Step 5: Filter nodes based on aggregate conditions
        var aggregateDictionary = new Dictionary<Type, object>();
        filtered = FilterByAggregateConditions(filtered, aggregateDictionary, stream);

        return filtered.Distinct().ToList();
    }

    private List<INode> MapEventsToNodes(List<object> allEvents, BProcess process)
    {
        return allEvents
            .Select(e => process.AllDistinctCommands
                .FirstOrDefault(cmd => cmd.ProducingEvents.Any(ev => ev.Name == e.GetType().Name)))
            .Where(node => node is not null)
            .Cast<INode>()
            .ToList();
    }

    private List<(INode node, int count)> GroupNodesByType(List<INode> nodes)
    {
        var storedNodes = nodes.Where(node => node is not IOptional).ToList();
        return storedNodes
            .GroupBy(node => node is IMulti ? node.CommandType.Name : Guid.NewGuid().ToString())
            .Select(group => (node: group.First(), count: group.Count()))
            .ToList();
    }

    private List<List<INode>> FilterPossiblePaths(List<(INode node, int count)> storedGroups)
    {
        var ct = storedGroups.Count;
        return _processConfig.AllPossibles
            .Where(path => path
                .Except(path.Where(node => node is IOptional))
                .Take(ct)
                .SequenceEqual(storedGroups.Select(g => g.node), new NodeEqualityComparer()))
            .ToList();
    }

    private List<INode> ExtractNextSteps(List<List<INode>> possiblePaths, List<(INode node, int count)> storedGroups)
    {
        return possiblePaths
            .SelectMany(path =>
            {
                var lastGroupCommandType = storedGroups.Last().node.CommandType;
                var lastIndex = path.IndexOf(path.First(node => node.CommandType == lastGroupCommandType));

                var nextSteps = path[lastIndex].NextSteps;
                if (nextSteps != null)
                    return nextSteps
                        .Union(path.Where((node, index) => index <= lastIndex && node is IOptional or IMulti));

                return path.Where((node, index) => index <= lastIndex && node is IOptional or IMulti);
            })
            .ToList();
    }

    private List<INode> FilterByAggregateConditions(List<INode> nodes, Dictionary<Type, object> aggregateDictionary, List<object> allEvents)
    {
        return nodes.Where(node =>
        {
            if (node.AggregateConditions == null || node.AggregateConditions.Count == 0)
                return true;

            var uniqueAggTypes = node.AggregateConditions
                .Select(ac => ac.ConditionalAggregateType)
                .Distinct();

            foreach (var aggType in uniqueAggTypes)
            {
                if (!aggregateDictionary.ContainsKey(aggType))
                {
                    var aggregateStream = _repository.AggregateOrDefaultStreamFromRegistry(aggType, allEvents);
                    aggregateDictionary.Add(aggType, aggregateStream);
                }
            }

            return node.AggregateConditions.All(ac =>
                ac.EvaluateAggregateCondition(aggregateDictionary[ac.ConditionalAggregateType]));
        }).ToList();
    }

    public async Task AppendUncommittedToDb(CancellationToken ct)
    {
        var headers = new Dictionary<string, object> { { "AggregateType", _aggregateName } };
        await _repository.AppendEvents(_aggregateId, _uncommittedEvents.ToArray(), _isNewProcess, headers, ct);
        _isNewProcess = false;
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await _repository.SaveChangesAsync(ct);
    }
}

public class NodeEqualityComparer : IEqualityComparer<INode>
{
    public bool Equals(INode? x, INode? y)
    {
        return x?.CommandType == y?.CommandType;
    }

    public int GetHashCode(INode obj) => obj.GetHashCode();
}