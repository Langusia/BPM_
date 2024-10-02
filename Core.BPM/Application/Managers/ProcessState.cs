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
    public List<MutableTuple<string, INode?>> ProgressedPath { get; private set; }
    public BProcess ProcessConfig { get; }
    public INode? CurrentStep { get; private set; }
    public Type CommandOrigin { get; private set; }

    public bool ValidateOrigin() => ValidateFor(CommandOrigin);
    public bool ValidateFor<TCommand>() where TCommand : IBaseRequest => ValidateFor(typeof(TCommand));

    public bool ValidateFor(Type commandType)
    {
        var matchintNodes = ProcessConfig.GetNodes(commandType);
        return matchintNodes.Any(x => x.ValidatePlacement(_allEvents.Select(x => new MutableTuple<string, INode?>(x, null)).ToList(), CurrentStep));
    }

    private void InitializeProcessState(Type? commandOrigin = null)
    {
        _allEvents = Aggregate.PersistedEvents.Union(Aggregate.UncommittedEvents.Select(x => x.GetType().Name)).ToList();
        ProgressedPath = _allEvents.Select(x => new MutableTuple<string, INode?>(x, null)).ToList();
        CurrentStep = ProcessConfig.FindLastValidNode(ProgressedPath.Select(x => x.Item1).ToList());
        CommandOrigin = commandOrigin;
    }

    public bool AppendEvent(Action<T> action)
    {
        var nextSteps = CurrentStep!.NextSteps;
        if (nextSteps is null)
            return false;

        action(Aggregate);
        if (CurrentStep is not AnyTimeNode)
            if (nextSteps.All(x => x.ProducingEvents.All(z => z.Name != Aggregate.LastUncommitedEvent?.GetType().Name)))
                return false;

        _allEvents = Aggregate.PersistedEvents.Union(Aggregate.UncommittedEvents.Select(x => x.GetType().Name)).ToList();
        ProgressedPath = _allEvents.Select(x => new MutableTuple<string, INode?>(x, null)).ToList();
        CurrentStep = ProcessConfig.FindLastValidNode(ProgressedPath.Select(x => x.Item1).ToList());
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