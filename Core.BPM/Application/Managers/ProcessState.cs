using Core.BPM.Application.Events;
using Core.BPM.BCommand;
using Core.BPM.Configuration;
using Core.BPM.Interfaces;
using Core.BPM.Nodes;
using MediatR;

namespace Core.BPM.Application.Managers;

public class ProcessState<T> where T : Aggregate
{
    public ProcessState(T aggregate)
    {
        Aggregate = aggregate;
        ProcessConfig = BProcessGraphConfiguration.GetConfig<T>();
        InitializeProcessState();
    }

    public ProcessState(T aggregate, BProcess config, Type commandOrigin)
    {
        Aggregate = aggregate;
        ProcessConfig = config;
        InitializeProcessState(commandOrigin);
    }

    public T Aggregate { get; }
    private List<string> _allEvents = [];
    public List<string> ProgressedPath { get; private set; }
    public BProcess ProcessConfig { get; }
    public INode? CurrentStep { get; private set; }
    public Type CommandOrigin { get; private set; }

    public bool ValidateOrigin() => ValidateFor(CommandOrigin);
    public bool ValidateFor<TCommand>() where TCommand : IBaseRequest => ValidateFor(typeof(TCommand));

    public bool ValidateFor(Type commandType)
    {
        if (_allEvents.Any(x => x == nameof(ProcessFailed)))
            return false;

        var matchingNodes = ProcessConfig.GetNodes(commandType);
        return matchingNodes.Any(x => x.ValidatePlacement(ProcessConfig, _allEvents, CurrentStep));
    }

    private void InitializeProcessState(Type? commandOrigin = null)
    {
        _allEvents = Aggregate.PersistedEvents.Union(Aggregate.UncommittedEvents.Select(x => x.GetType().Name)).ToList();
        ProgressedPath = _allEvents;
        CurrentStep = ProcessConfig.FindLastValidNode(ProgressedPath);
        CommandOrigin = commandOrigin;
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

public class MutableTuple<T1, T2>
{
    public T1 Item1 { get; set; }
    public T2? Item2 { get; set; }

    public MutableTuple(T1 item1, T2 item2)
    {
        Item1 = item1;
        Item2 = item2;
    }
}