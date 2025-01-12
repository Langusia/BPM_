using Core.BPM.Application.Events;
using Core.BPM.BCommand;
using Core.BPM.Configuration;
using Core.BPM.Interfaces;
using Core.BPM.Nodes;
using Core.BPM.Persistence;
using MediatR;

namespace Core.BPM.Application.Managers;

public class Process : IProcess
{
    private readonly IBpmRepository _repository;
    private readonly List<object> _storedEvents = [];
    private Queue<object> _inMemoryEvents = [];
    private Guid _aggregateId;
    private readonly BProcess _processConfig;
    private bool _isNewProcess;

    public Process(Guid aggregateId, string rootAggregateName, bool isNewProcess, IEnumerable<object>? events, IBpmRepository repository)
    {
        _processConfig = BProcessGraphConfiguration.GetConfig(rootAggregateName) ?? throw new Exception();
        if (events is not null)
            _storedEvents = events.ToList();

        _aggregateId = aggregateId;
        _isNewProcess = isNewProcess;

        _repository = repository;
    }


    public T? AggregateAs<T>(bool includeUnsaved = true) where T : Aggregate
    {
        var stream = _storedEvents;
        if (includeUnsaved)
            stream = stream.Union(_inMemoryEvents).ToList();

        if (stream.Any())
        {
            var aggregate = (T)_repository.AggregateStreamFromRegistry(typeof(T), stream);
            aggregate.Id = _aggregateId;
            return aggregate;
        }

        return null;
    }

    public object AppendEvents(params object[] events)
    {
        throw new NotImplementedException();
    }

    public bool Validate()
    {
        throw new NotImplementedException();
    }

    public List<INode>? UnlockedPaths(List<object> events, BProcess process)
    {
        var allEvents = events.Select(x => x.GetType().Name);
        //map events to Nodes
        var rs = allEvents.Select(x =>
                process.AllDistinctCommands.FirstOrDefault(z => z.ProducingEvents.Any(e => e.Name == x)))
            .Where(x => x is not null).ToList();

        var storedMandatoryNodes = rs.Where(x => x is not IOptional).ToList();

        var storedGroups = storedMandatoryNodes.GroupBy(x => x is IMulti ? x.CommandType.Name : Guid.NewGuid().ToString())
            .Select(x => new
            {
                node = x.First(),
                count = x.Count()
            });

        var ct = storedGroups.Count();

        var curs = _processConfig.AllPossibles.Where(x =>
            x.Except(x.Where(node => node is IOptional))
                .Take(ct)
                .SequenceEqual(storedGroups.Select(c => c.node).ToList(), new NodeEqualityComparer())).ToList();

        var filtered = curs.SelectMany(x =>
        {
            var ix = x.IndexOf(x.First(z => z.CommandType == storedGroups.Last().node.CommandType));
            return x[ix].NextSteps.Union(x.Where((value, index) => index <= ix && value is IOptional or IMulti));
        }).ToList();

        Dictionary<Type, object> aggregateDictionary = [];

        filtered = filtered.Where(x =>
        {
            if (x.AggregateConditions == null || x.AggregateConditions.Count == 0)
                return true;

            var nodeAggTypes = x.AggregateConditions
                .Select(ac => ac.ConditionalAggregateType)
                .Distinct();

            foreach (var predicateAggregateType in nodeAggTypes)
            {
                if (!aggregateDictionary.ContainsKey(predicateAggregateType))
                {
                    var instancedAggregate = _repository.AggregateStreamFromRegistry(predicateAggregateType, allEvents);
                    aggregateDictionary.Add(predicateAggregateType, instancedAggregate);
                }
            }

            return x.AggregateConditions.All(ac =>
                ac.EvaluateAggregateCondition(aggregateDictionary[ac.ConditionalAggregateType]));
        }).ToList();

        return filtered;
    }
}

public class ProcessState<T> where T : Aggregate
{
    public ProcessState(T aggregate, object originalAggregate)
    {
        Aggregate = aggregate;
        OriginalAggregate = originalAggregate;
        ProcessConfig = BProcessGraphConfiguration.GetConfig<T>();
        InitializeProcessState();
    }

    public ProcessState(T aggregate, object originalAggregate, BProcess config, Type commandOrigin)
    {
        Aggregate = aggregate;
        OriginalAggregate = originalAggregate;
        ProcessConfig = config;
        InitializeProcessState(commandOrigin);
    }

    private void InitializeProcessState(Type? commandOrigin = null)
    {
        Options = BProcessStepConfiguration.GetConfig(ProcessConfig.ProcessType);
        _allEvents = Aggregate.PersistedEvents.Union(Aggregate.UncommittedEvents.Select(x => x.GetType().Name)).ToList();
        ProgressedPath = _allEvents;
        CurrentStep = ProcessConfig.FindLastValidNode(ProgressedPath);
        CommandOrigin = commandOrigin;
    }

    public T Aggregate { get; }
    public object OriginalAggregate { get; }
    private List<string> _allEvents = [];
    public List<string> ProgressedPath { get; private set; }
    public BProcess ProcessConfig { get; }
    public INode? CurrentStep { get; private set; }
    public StepOptions? Options { get; set; }
    public Type CommandOrigin { get; private set; }
    public List<INode> Map { get; private set; }

    public bool ValidateOrigin() => ValidateFor(CommandOrigin);
    public bool ValidateFor<TCommand>() where TCommand : IBaseRequest => ValidateFor(typeof(TCommand));

    public bool ValidateFor(Type commandType)
    {
        if (_allEvents.Any(x => x == nameof(ProcessFailed)))
            return false;

        var matchingNodes = ProcessConfig.GetNodes(commandType);

        if (!matchingNodes.Any(x => x.ValidatePlacement(ProcessConfig, _allEvents, CurrentStep)))
            return false;

        var valid = Options?.EvaluateAggregateCondition(OriginalAggregate);

        return valid ?? true;
    }


    public bool AppendEvent(params BpmEvent[] evts)
    {
        if (_allEvents.Any(x => x == nameof(ProcessFailed)))
            return false;

        if (evts.Any(x => !BProcessGraphConfiguration.GetCommandProducer(CommandOrigin).EventTypes.Contains(x.GetType())))
            return false;

        foreach (var evt in evts)
            Aggregate.Enqueue(evt);

        _allEvents = Aggregate.PersistedEvents.Union(Aggregate.UncommittedEvents.Select((Func<object, string>)(x => x.GetType().Name))).ToList();
        ProgressedPath = _allEvents;
        CurrentStep = ProcessConfig.FindLastValidNode(ProgressedPath.ToList());
        return true;
    }

    public bool Fail(string description)
    {
        Aggregate.Enqueue(new ProcessFailed(description));

        _allEvents = Aggregate.PersistedEvents.Union(Aggregate.UncommittedEvents.Select((Func<object, string>)(x => x.GetType().Name))).ToList();
        ProgressedPath = _allEvents;
        CurrentStep = ProcessConfig.FindLastValidNode(ProgressedPath.ToList());
        return true;
    }
}