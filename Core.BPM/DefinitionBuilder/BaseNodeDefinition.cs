using Core.BPM.Interfaces;

namespace Core.BPM.DefinitionBuilder;

public class BaseNodeDefinition(INode firstNode, BProcess process)
{
    protected readonly BProcess Process = process;
    protected INode CurrentNode = firstNode;
    protected List<INode> CurrentBranchInstances = [firstNode];
    protected bool Lock;


    public BProcess GetProcess() => Process;
    public INode GetCurrent() => CurrentNode;
    public INode GetRoot() => firstNode;
    public List<INode> GetBranchInstances() => CurrentBranchInstances;

    public List<INode> AddBranchInstance(INode node)
    {
        node.PrevSteps = firstNode.PrevSteps;
        CurrentBranchInstances.Add(node);
        return CurrentBranchInstances;
        //node.PrevSteps ??= [];
        //node.PrevSteps?.Add(rootNode);
        //rootNode.NextSteps ??= [];
        //rootNode.NextSteps?.Add(node);
        //return rootNode.NextSteps!;
    }

    public INode SetRoot(INode node)
    {
        firstNode = node;
        return firstNode;
    }

    public INode SetCurrent(INode node)
    {
        CurrentNode = node;
        return CurrentNode;
    }


    protected void SetRootData(BaseNodeDefinition builder)
    {
        var nextBranchInstances = builder.GetBranchInstances();
        foreach (var nextBuilderBranchInstance in nextBranchInstances)
        {
            nextBuilderBranchInstance.AddPrevSteps(CurrentBranchInstances);
        }

        foreach (var currentBranchInstance in CurrentBranchInstances)
        {
            currentBranchInstance.AddNextSteps(nextBranchInstances);
        }

        CurrentBranchInstances = builder.GetBranchInstances();
    }

    protected void Merge(BaseNodeDefinition builder)
    {
        var nextBranchInstances = builder.GetBranchInstances();
        foreach (var nextBuilderBranchInstance in nextBranchInstances)
        {
            nextBuilderBranchInstance.AddPrevSteps(CurrentBranchInstances);
        }

        foreach (var currentBranchInstance in CurrentBranchInstances)
        {
            currentBranchInstance.AddNextSteps(nextBranchInstances);
        }

        CurrentBranchInstances = builder.GetBranchInstances();
    }
}