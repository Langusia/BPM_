using Core.BPM.Configuration;
using Core.BPM.Interfaces;
using Core.BPM.Nodes;
using Core.BPM.Persistence;
using MediatR;

namespace Core.BPM.Application.Managers;

public class Process : IProcess, IProcessStore
{
    public Guid Id { get; }
    public string AggregateName { get; }

    private readonly BProcess _processConfig;
    private readonly IBpmRepository _repository;

    private readonly List<object> _storedEvents = [];
    private readonly Queue<object> _uncommittedEvents = [];

    private bool _isNewProcess;

    public Process(Guid aggregateId, string rootAggregateName, bool isNewProcess, IEnumerable<object>? events, IEnumerable<object>? upcomingEvents, IBpmRepository repository)
    {
        _processConfig = BProcessGraphConfiguration.GetConfig(rootAggregateName) ?? throw new Exception();
        if (events is not null)
            _storedEvents = events.ToList();
        if (upcomingEvents != null)
            foreach (var upcomingEvent in upcomingEvents)
            {
                _uncommittedEvents.Enqueue(upcomingEvent);
            }

        Id = aggregateId;
        AggregateName = rootAggregateName;
        _isNewProcess = isNewProcess;

        _repository = repository;
    }


    public T AggregateAs<T>(bool includeUncommitted = true) where T : Aggregate
    {
        var stream = _storedEvents;
        if (includeUncommitted)
            stream = stream.Union(_uncommittedEvents).ToList();

        var aggregate = (T)_repository.AggregateStreamFromRegistry(typeof(T), stream);
        aggregate.Id = Id;
        return aggregate;
    }

    public T? AggregateOrNullAs<T>(bool includeUncommittedEvents = true) where T : Aggregate
    {
        throw new NotImplementedException();
    }

    public T? AggregateOrNullAsAs<T>(bool includeUncommitted = true) where T : Aggregate
    {
        try
        {
            var stream = _storedEvents;
            if (includeUncommitted)
                stream = stream.Union(_uncommittedEvents).ToList();

            var aggregate = (T)_repository.AggregateStreamFromRegistry(typeof(T), stream);
            aggregate.Id = Id;
            return aggregate;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public bool TryAggregateAs<T>(out T? aggregate, bool includeUncommitted = true) where T : Aggregate
    {
        aggregate = null;
        try
        {
            var stream = _storedEvents;
            if (includeUncommitted)
                stream = stream.Union(_uncommittedEvents).ToList();

            aggregate = (T)_repository.AggregateStreamFromRegistry(typeof(T), stream);
            aggregate.Id = Id;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }


    public bool AppendEvents(params object[] events)
    {
        //TODO: checking must be more advanced
        var filtered = UnlockedPaths();
        if (filtered.All(f => events.Select(e => e.GetType().Name).Any(z => !f.ProducingEvents.Select(c => c.Name).Contains(z))))
            return false;

        foreach (var @event in events)
        {
            _uncommittedEvents.Enqueue(@event);
        }

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

                var nextSteps = new List<INode>(); // Replace NodeType with the actual node type
                for (int i = lastIndex + 1; i < path.Count; i++)
                {
                    var node = path[i];

                    if (node is IOptional)
                    {
                        nextSteps.Add(node);
                    }
                    else
                    {
                        nextSteps.Add(node);
                        break;
                    }
                }

                if (nextSteps != null)
                    return nextSteps
                        .Union(path.Where((node, index) => index <= lastIndex && node is IOptional or IMulti));

                return path.Where((node, index) => index <= lastIndex && node is IOptional or IMulti);
            }).ToList();
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
        var headers = new Dictionary<string, object> { { "AggregateType", AggregateName } };
        await _repository.AppendEvents(Id, _uncommittedEvents.ToArray(), _isNewProcess, headers, ct);
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