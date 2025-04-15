using System;
using System.Collections.Generic;
using System.Linq;
using Core.BPM.AggregateConditions;
using Core.BPM.Attributes;
using Core.BPM.Evaluators;
using Core.BPM.Evaluators.Factory;
using Core.BPM.Interfaces;

namespace Core.BPM.Nodes;

public abstract class NodeBase : INode
{
    private readonly INodeEvaluatorFactory _nodeEvaluatorFactory;
    private readonly Dictionary<(INode, int), (bool, List<INode>)> _cache = new();

    public NodeBase(Type commandType, Type processType, INodeEvaluatorFactory nodeEvaluatorFactory)
    {
        _nodeEvaluatorFactory = nodeEvaluatorFactory;
        if (commandType != typeof(GroupNode) && commandType != typeof(ConditionalNode) && commandType != typeof(GuestProcessNode))
        {
            var producer = GetCommandProducer(commandType);
            if (producer is null)
                throw new InvalidOperationException($"Command {commandType.Name} does not have a producer attribute.");
            if (producer.EventTypes.Length == 0)
                throw new InvalidOperationException($"Command {commandType.Name} does not have any event types.");
            ProducingEvents = producer.EventTypes.ToList();
        }

        CommandType = commandType;
        ProcessType = processType;
    }

    public Type CommandType { get; }
    public int NodeLevel { get; set; }

    public Type ProcessType { get; }

    public List<Type> ProducingEvents { get; }
    public List<IAggregateCondition>? AggregateConditions { get; set; }


    public List<INode> NextSteps { get; set; } = [];


    public virtual INode? FindNextNode(string eventName)
    {
        return NextSteps.FirstOrDefault(step => step.ProducingEvents.Any(e => e.Name == eventName));
    }

    public virtual INodeStateEvaluator GetEvaluator() => _nodeEvaluatorFactory.CreateEvaluator(this);
    public virtual bool ContainsEvent(object @event) => GetCommandProducer(CommandType).EventTypes.Select(x => x.Name).Contains(@event.GetType().Name);

    public bool ContainsEvent(List<object> events)
    {
        var evts = GetCommandProducer(CommandType).EventTypes.Select(x => x.Name);
        return events.Select(x => x.GetType().Name).Any(x => evts.Contains(x));
    }

    public virtual bool ContainsNodeEvent(BpmEvent @event)
    {
        //var bpmEvents = storedEvents.OfType<BpmEvent>().ToList();
        //return bpmEvents?.Any(node.ContainsNodeEvent) ?? storedEvents.Except(bpmEvents ?? Enumerable.Empty<object>()).Any(node.ContainsEvent);
        return GetCommandProducer(CommandType).EventTypes.Select(x => x.Name).Contains(@event.GetType().Name) &&
               NodeLevel == @event.NodeId;
    }


    public List<INode> GetAllNodes()
    {
        var root = this;
        HashSet<INode> visitedNodes = new();
        List<INode> allNodes = new();

        void Traverse(INode node)
        {
            if (node == null || visitedNodes.Contains(node))
                return;

            visitedNodes.Add(node);
            allNodes.Add(node);

            foreach (var next in node.NextSteps ?? [])
            {
                Traverse(next);
            }
        }

        Traverse(root);
        return allNodes;
    }

    public void AddNextStep(INode node)
    {
        NextSteps ??= [];
        NextSteps.Add(node);
    }

    public List<INode>? PrevSteps { get; set; }

    public void SetPrevSteps(List<INode>? nodes)
    {
        if (nodes is not null)
        {
            PrevSteps = null;
            PrevSteps = nodes;
        }
    }


    protected static BpmProducer GetCommandProducer(Type commandType)
    {
        return (BpmProducer)commandType.GetCustomAttributes(typeof(BpmProducer), false).FirstOrDefault()!;
    }

    private INode _currNext;
    private bool _isComplete;

    public (bool isComplete, List<INode> availableNodes) GetCheckBranchCompletionAndGetAvailableNodesFromCache(List<object> storedEvents,
        List<(string, INode, bool isCompleted, bool canExec, List<INode> availableNodes)>? res = null)
    {
        int eventHash = storedEvents?.Count > 0 ? storedEvents.GetHashCode() : 0;
        var cacheKey = (this, eventHash);

        if (_cache.TryGetValue(cacheKey, out var cachedResult))
            return cachedResult;

        return this.CheckBranchCompletionAndGetAvailableNodes(this, storedEvents, res);
    }

    public (bool isComplete, List<INode> availableNodes) CheckBranchCompletionAndGetAvailableNodes(INode start, List<object> storedEvents,
        List<(string, INode, bool isCompleted, bool canExec, List<INode> availableNodes)>? res = null)
    {
        List<INode> availableNodes = [];
        bool isAnyBranchComplete = false;

        void Traverse(INode rootNode, INode node)
        {
            var evaluator = node.GetEvaluator();
            bool nodeCompleted = evaluator.IsCompleted(storedEvents);
            if (node.NextSteps == null || node.NextSteps.Count == 0)
                isAnyBranchComplete |= nodeCompleted;

            var canExecute = evaluator.CanExecute(rootNode, storedEvents);
            if (canExecute.canExec)
            {
                var availables = canExecute.availableNodes.ToList();
                var nodesToAdd = availables.Where(x => !availableNodes.Contains(x));
                availableNodes.AddRange(nodesToAdd);
            }

            res?.Add(new ValueTuple<string, INode, bool, bool, List<INode>>(node.CommandType.Name, node, nodeCompleted, canExecute.canExec, canExecute.availableNodes));
            foreach (var next in node.NextSteps ?? [])
            {
                Traverse(node, next);
            }
        }

        Traverse(start, start);

        return (isAnyBranchComplete, availableNodes);
    }
}