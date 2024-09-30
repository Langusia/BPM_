using Core.BPM.Application.Exceptions;
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
        InitializeProcessState();
    }

    public ProcessState(T aggregate, BProcess config, List<string> persistedEvents, Type commandOrigin)
    {
        Aggregate = aggregate;
        ProcessConfig = config;
        _persistedEvents = persistedEvents;
        InitializeProcessState(commandOrigin);
    }

    public T Aggregate { get; }
    private readonly List<string> _persistedEvents;
    private List<string> _inMemoryEvents = [];
    public List<MutableTuple<string, INode?>> ProgressedPath { get; private set; }
    public BProcess ProcessConfig { get; }
    public INode? CurrentStep { get; private set; }
    public Type CommandOrigin { get; private set; }

    public bool ValidateOrigin() => ValidateFor(CommandOrigin);
    public bool ValidateFor<TCommand>() where TCommand : IBaseRequest => ValidateFor(typeof(TCommand));

    public bool ValidateFor(Type commandType)
    {
        var nodeFromConfig = ProcessConfig.MoveTo(commandType);
        if (nodeFromConfig == null || nodeFromConfig.Count == 0)
        {
            throw new ProcessStateException($"No valid node found for the command type: {commandType.Name}");
        }

        return nodeFromConfig.FirstOrDefault()?.Validate(ProgressedPath, CurrentStep) ?? false;
    }

    private void InitializeProcessState(Type? commandOrigin = null)
    {
        _inMemoryEvents = Aggregate.PersistedEvents.Union(Aggregate.UncommittedEvents.Select(x => x.GetType().Name)).ToList();
        ProgressedPath = _inMemoryEvents.Select(x => new MutableTuple<string, INode?>(x, null)).ToList();
        CurrentStep = FindCurrentNode();
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

        _inMemoryEvents = _persistedEvents.Union(Aggregate.UncommittedEvents.Select(x => x.GetType().Name)).ToList();
        ProgressedPath = _inMemoryEvents.Select(x => new MutableTuple<string, INode?>(x, null)).ToList();
        CurrentStep = FindCurrentNode();
        return true;
    }

    private INode? FindCurrentNode()
    {
        //if (!_inMemoryEvents.Any())
        //    return ProcessConfig.RootNode;

        var currentStep = ProcessConfig.RootNode;
        ProgressedPath.FirstOrDefault()!.Item2 = currentStep;

        foreach (var eventName in _inMemoryEvents)
        {
            currentStep = currentStep?.FindNextNode(eventName);
            if (currentStep == null) return null;

            ProgressedPath.FirstOrDefault(x => x.Item1 == eventName)!.Item2 = currentStep;
        }

        return currentStep;
    }


    public INode? FindNode(Type searchCommand)
    {
        var currentStep = ProcessConfig.RootNode;
        if (currentStep?.CommandType == searchCommand)
            return currentStep;

        foreach (var eventName in _inMemoryEvents)
        {
            currentStep = currentStep?.FindNextNode(eventName);
            if (currentStep == null) return null;

            if (currentStep.CommandType == searchCommand)
                return currentStep;
        }

        return null;
    }

    private Tuple<INode?, INode?> TraverseToEnd(Type? searchCommand = null)
    {
        if (!_inMemoryEvents.Any())
            return new Tuple<INode?, INode?>(ProcessConfig.RootNode, null);

        var currentStep = ProcessConfig.RootNode;
        ProgressedPath.FirstOrDefault()!.Item2 = currentStep;

        foreach (var eventName in _inMemoryEvents)
        {
            currentStep = currentStep?.FindNextNode(eventName);
            if (currentStep == null) return new Tuple<INode?, INode?>(null, null);

            ProgressedPath.FirstOrDefault(x => x.Item1 == eventName)!.Item2 = currentStep;
        }

        var foundNode = currentStep?.CommandType == searchCommand ? currentStep : null;
        return new Tuple<INode?, INode?>(currentStep, foundNode);
    }
}