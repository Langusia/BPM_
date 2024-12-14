using Core.BPM.AggregateConditions;
using Core.BPM.Attributes;
using Core.BPM.BCommand;
using Core.BPM.Interfaces;
using Core.BPM.Nodes;

namespace Core.BPM;

public abstract class NodeBase : INode
{
    public NodeBase(Type commandType, Type processType)
    {
        var producer = GetCommandProducer(commandType);
        if (producer is null)
            throw new InvalidOperationException($"Command {commandType.Name} does not have a producer attribute.");
        if (producer.EventTypes.Length == 0)
            throw new InvalidOperationException($"Command {commandType.Name} does not have any event types.");

        CommandType = commandType;
        ProcessType = processType;
        ProducingEvents = producer.EventTypes.ToList();
    }

    public Type CommandType { get; }
    public string CommandName => CommandType.Name;

    public Type ProcessType { get; }
    public StepOptions Options { get; set; }

    public List<Type> ProducingEvents { get; }
    public List<IAggregateCondition>? AggregateConditions { get; set; }


    public List<INode> NextSteps { get; set; } = [];

    public void AddAggregateCondition<TAggregate>(Predicate<TAggregate> predicate) where TAggregate : Aggregate
    {
        AggregateConditions ??= new List<IAggregateCondition>();
        AggregateConditions.Add(new AggregateCondition<TAggregate>(predicate));
    }

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

    public List<INode> PrevSteps { get; set; }

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
        PrevSteps = nodes;
    }

    public abstract bool ValidatePlacement(BProcess process, List<string> savedEvents, INode? currentNode);

    protected static BpmProducer GetCommandProducer(Type commandType)
    {
        return (BpmProducer)commandType.GetCustomAttributes(typeof(BpmProducer), false).FirstOrDefault()!;
    }

    private INode _currNext;

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