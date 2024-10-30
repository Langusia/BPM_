using Core.BPM.Application.Managers;
using Core.BPM.Attributes;
using Core.BPM.BCommand;
using Core.BPM.Interfaces;

namespace Core.BPM;

public abstract class NodeBase : INode, INodeState
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
    public BpmEventOptions Options { get; set; }
    public List<Type> ProducingEvents { get; }


    public List<INode> NextSteps { get; set; } = [];

    public virtual INode? FindNextNode(string eventName)
    {
        return NextSteps.FirstOrDefault(step => step.ProducingEvents.Any(e => e.Name == eventName));
    }

    public void AddNextStep(INode node)
    {
        NextSteps ??= [];
        NextSteps.Add(node);
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

    public List<INode> PrevSteps { get; set; }

    public void AddPrevStep(INode node)
    {
        PrevSteps ??= [];
        PrevSteps.Add(node);
    }

    public abstract bool ValidatePlacement(List<string> savedEvents, INode? currentNode);

    protected static BpmProducer GetCommandProducer(Type commandType)
    {
        return (BpmProducer)commandType.GetCustomAttributes(typeof(BpmProducer), false).FirstOrDefault()!;
    }

    private INode _currNext;

    protected void GetLastNodes(List<INode> lastNodes, INode start)
    {
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

    public bool DefinitionValidated { get; set; }
    public bool CanAppend { get; set; }
}