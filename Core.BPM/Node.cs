using Core.BPM.BCommand;
using Core.BPM.Interfaces;
using Core.BPM.MediatR.Attributes;

namespace Core.BPM;

public class Node(Type commandType, Type processType) : INode
{
    public List<string> LoadEvents()
        => ((BpmProducer)commandType.GetCustomAttributes(typeof(BpmProducer), false).FirstOrDefault()!).EventTypes.Select(x => x.Name).ToList();

    public Type CommandType { get; } = commandType;
    public Type ProcessType { get; } = processType;
    public BpmEventOptions Options { get; set; }

    public List<INode> NextSteps { get; set; } = [];

    public void AddNextStep(INode node)
    {
        NextSteps ??= [];
        NextSteps.Add(node);
    }

    public List<INode> PrevSteps { get; set; }

    public void AddPrevStep(INode node)
    {
        PrevSteps ??= [];
        PrevSteps.Add(node);
    }


    private INode _currNext;

    private void GetLastNodes(List<INode> lastNodes, INode start)
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
}