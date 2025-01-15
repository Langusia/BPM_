using MediatR;
using Core.BPM.BCommand;
using Core.BPM.Interfaces;
using Core.BPM.Configuration;
using Core.BPM.Application.Events;

namespace Core.BPM.Application.Managers;

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