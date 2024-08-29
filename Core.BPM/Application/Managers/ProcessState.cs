using Core.BPM.Configuration;
using Core.BPM.Interfaces;
using Core.BPM.Nodes;
using MediatR;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Core.BPM.Application.Managers;

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

public class ProcessState<T> where T : Aggregate
{
    public ProcessState(T aggregate)
    {
        Aggregate = aggregate;
        ProcessConfig = BProcessGraphConfiguration.GetConfig<T>();
        CurrentStep = TraverseToEnd().Item1;
        _inMemoryEvents = aggregate.UncommittedEvents.Select(x => x.GetType().Name).ToList();
        ProgressedPath = _inMemoryEvents.Select(x => new MutableTuple<string, INode?>(x, null)).ToList();
    }

    public ProcessState(T aggregate, BProcess config, List<string> persistedEvents, Type commandOrigin)
    {
        Aggregate = aggregate;
        ProcessConfig = config;
        _persistedEvents = _inMemoryEvents = persistedEvents;
        ProgressedPath = _inMemoryEvents.Select(x => new MutableTuple<string, INode?>(x, null)).ToList();
        CurrentStep = TraverseToEnd().Item1;
        CommandOrigin = commandOrigin;
    }

    public T Aggregate { get; }
    private readonly List<string> _persistedEvents;
    private List<string> _inMemoryEvents = [];
    public List<MutableTuple<string, INode?>> ProgressedPath { get; private set; }
    public BProcess ProcessConfig { get; }
    public INode? CurrentStep { get; private set; }
    public Type CommandOrigin { get; }

    public bool ValidateOrigin() => ValidateFor(CommandOrigin);
    public bool ValidateFor<TCommand>() where TCommand : IBaseRequest => ValidateFor(typeof(TCommand));

    public bool ValidateFor(Type commandType)
    {
        var nodeFromConfig = ProcessConfig.MoveTo(commandType);
        if (nodeFromConfig is null || nodeFromConfig.FirstOrDefault() is null || nodeFromConfig.Count == 0)
            return false;

        return nodeFromConfig.FirstOrDefault()!.Validate(ProgressedPath, CurrentStep);
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

        _inMemoryEvents = _persistedEvents.Union(Aggregate.UncommittedEvents.Select(x => x.GetType().Name)).ToList();
        ProgressedPath = _inMemoryEvents.Select(x => new MutableTuple<string, INode?>(x, null)).ToList();
        CurrentStep = TraverseToEnd().Item1;
        return true;
    }

    private Tuple<INode?, INode?> TraverseToEnd(Type? searchCommand = null)
    {
        if (_inMemoryEvents is null || _inMemoryEvents.Count == 0)
            return new Tuple<INode?, INode?>(ProcessConfig.RootNode, null);
        var persEventsCopy = new List<string>(_inMemoryEvents);
        var currentStep = ProcessConfig.RootNode;
        if (persEventsCopy.Count == 0)
            return null;

        if (!ProcessConfig.RootNode.ProducingEvents.Any(x => x.Name == persEventsCopy.FirstOrDefault()))
            return null;

        ProgressedPath.FirstOrDefault()!.Item2 = ProcessConfig.RootNode;
        persEventsCopy.RemoveAt(0);
        var foundNode = currentStep.CommandType == searchCommand ? currentStep : null;
        if (persEventsCopy.Count == 0)
            return new Tuple<INode?, INode?>(currentStep, foundNode);

        var matched = true;
        while (matched && persEventsCopy.Count > 0)
        {
            foreach (var step in currentStep.NextSteps!)
            {
                var @event = persEventsCopy.FirstOrDefault();
                if (step.ProducingEvents.Any(x => x.Name == @event))
                {
                    persEventsCopy.RemoveAll(x => x == @event);
                    if (step.CommandType == searchCommand)
                        foundNode = step;

                    currentStep = step;
                    foreach (var mutableTuple in ProgressedPath.Where(x => x.Item1 == @event))
                    {
                        mutableTuple.Item2 = currentStep;
                    }

                    matched = true;
                    break;
                }

                if (step is OptionalNode)
                {
                    if (step.CommandType == searchCommand)
                        foundNode = step;

                    currentStep = step;
                    break;
                }

                matched = false;
            }

            if (!matched)
            {
                currentStep = null;
                break;
            }
        }

        return new Tuple<INode?, INode?>(currentStep, foundNode);
    }
}