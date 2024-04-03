using Core.BPM.Interfaces;

namespace Core.BPM;

public class Node(Type commandType, Type processType) : INode
{
    public Type CommandType { get; } = commandType;
    public Type ProcessType { get; } = processType;

    public List<INode> NextSteps { get; set; } = new List<INode>();

    public void AddNextStep(INode node)
    {
        NextSteps ??= new List<INode>();
        NextSteps.Add(node);
    }

    public List<INode> PrevSteps { get; set; }

    public void AddPrevStep(INode node)
    {
        PrevSteps ??= new List<INode>();
        PrevSteps.Add(node);
    }


    private INode currNext;

    private void GetLastNodes(List<INode> lastNodes, INode start)
    {
        foreach (var nextStep in start.NextSteps)
        {
            if (nextStep.NextSteps is null || nextStep.NextSteps.Count == 0)
                lastNodes.Add(nextStep);
            else
            {
                currNext = nextStep;
                GetLastNodes(lastNodes, currNext);
            }
        }

        currNext = null;
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