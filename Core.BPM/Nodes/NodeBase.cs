using Core.BPM.AggregateConditions;
using Core.BPM.Attributes;
using Core.BPM.BCommand;
using Core.BPM.Evaluators;
using Core.BPM.Exceptions;
using Core.BPM.Interfaces;

namespace Core.BPM.Nodes;

public abstract class NodeBase : INode
{
    public NodeBase(Type commandType, Type processType)
    {
        if (commandType != typeof(GroupNode))
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
    public string CommandName => CommandType.Name;

    public Type ProcessType { get; }
    public StepOptions Options { get; set; }

    public List<INode>? KeyNodes { get; set; }
    public List<Type> ProducingEvents { get; }
    public List<IAggregateCondition>? AggregateConditions { get; set; }


    public List<INode> NextSteps { get; set; } = [];

    public bool PlacementPreconditionMarked(List<string> savedEvents)
    {
        var preconditions = GetPlacementPreconditions();
        var preConditionEvts = preconditions.SelectMany(x => x.ProducingEvents.Select(x => x.Name)).ToList();
        return savedEvents.Any(s => preConditionEvts.Contains(s));
    }


    protected List<INode>? GetPlacementPreconditions()
    {
        // We will use a helper method to perform a depth-first search (DFS) and collect all non-optional preconditions.
        var preconditions = new List<INode>();
        GatherValidPlacementPreconditions(this, preconditions);

        // Return null if no non-optional preconditions are found
        return preconditions.Count > 0 ? preconditions : null;
    }

    private void GatherValidPlacementPreconditions(INode currentNode, List<INode> preconditions)
    {
        // Track if we've found at least one non-optional node at the current level
        bool foundNonOptional = false;

        // Traverse through all PrevSteps and look for non-optional nodes.
        foreach (var prevNode in currentNode.PrevSteps)
        {
            // If the previous node is not optional (does not implement IOptional), add it to the list
            if (!(prevNode is IOptional))
            {
                preconditions.Add(prevNode);
                foundNonOptional = true; // Mark that we found a non-optional node
            }
        }

        // If we found at least one non-optional node, continue recursion
        if (!foundNonOptional)
        {
            foreach (var prevNode in currentNode.PrevSteps)
            {
                GatherValidPlacementPreconditions(prevNode, preconditions);
                // Continue searching deeper in PrevSteps, but only for non-optional nodes
            }
        }
    }


    public virtual INode? FindNextNode(string eventName)
    {
        return NextSteps.FirstOrDefault(step => step.ProducingEvents.Any(e => e.Name == eventName));
    }

    public virtual INodeStateEvaluator GetEvaluator() => new NodeStateEvaluator(this);
    public virtual bool ContainsEvent(object @event) => GetCommandProducer(CommandType).EventTypes.Select(x => x.Name).Contains(@event.GetType().Name);

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

    public void AddNextSteps(List<INode>? nodes)
    {
        NextSteps ??= [];
        NextSteps.AddRange(nodes);
    }

    public void SetNextSteps(List<INode>? nodes)
    {
        NextSteps = nodes;
    }

    public void AddNextStepToTail(INode node)
    {
        if (NextSteps.Count == 0)
            NextSteps.Add(node);
        else
        {
            var tails = new List<INode>();
            GetLastNodes(tails, this);
            tails.Distinct().ToList().ForEach(x => x.AddNextStep(node));
        }
    }

    public List<INode> FetchLastNodes(INode node)
    {
        var tails = new List<INode>();
        GetLastNodes(tails, node);
        return tails.ToList();
    }

    public List<INode>? PrevSteps { get; set; }

    public void AddPrevStep(INode node)
    {
        PrevSteps ??= [];
        PrevSteps.Add(node);
    }

    public void AddPrevSteps(List<INode>? nodes)
    {
        PrevSteps ??= [];
        PrevSteps.AddRange(nodes);
    }

    public void SetPrevSteps(List<INode>? nodes)
    {
        PrevSteps = null;
        PrevSteps = nodes;
    }


    protected static BpmProducer GetCommandProducer(Type commandType)
    {
        return (BpmProducer)commandType.GetCustomAttributes(typeof(BpmProducer), false).FirstOrDefault()!;
    }

    private INode _currNext;
    private bool _isComplete;

    public (bool isComplete, List<INode> availableNodes) CheckBranchCompletionAndGetAvailableNodes(INode start, List<object> storedEvents)
    {
        List<INode> availableNodes = new();
        bool isAnyBranchComplete = false;

        void Traverse(INode node)
        {
            var evaluator = node.GetEvaluator();
            bool nodeCompleted = evaluator.IsCompleted(storedEvents);
            if (node.NextSteps == null || node.NextSteps.Count == 0)
                isAnyBranchComplete |= nodeCompleted;

            var canExecute = evaluator.CanExecute(storedEvents);
            if (canExecute.canExec)
                availableNodes.AddRange(canExecute.availableNodes.Where(x => !availableNodes.Contains(x)));

            foreach (var next in node.NextSteps ?? [])
            {
                Traverse(next);
            }
        }

        Traverse(start);

        return (isAnyBranchComplete, availableNodes);
    }


    protected void GetLastNodes(List<INode> lastNodes, INode start)
    {
        if (start.NextSteps is null || start.NextSteps.Count == 0)
        {
            lastNodes.Add(start);
            return;
        }

        foreach (var nextStep in start.NextSteps)
        {
            if (nextStep.NextSteps is null || nextStep.NextSteps.Count == 0)
                lastNodes.Add(nextStep);
            else
            {
                _currNext = nextStep;
                GetLastNodes(lastNodes, _currNext);
            }
        }

        _currNext = null;
    }
}