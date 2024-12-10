using Core.BPM.Interfaces;

namespace Core.BPM.DefinitionBuilder;

public class BaseNodeDefinition(INode firstNode, BProcess process)
{
    protected readonly BProcess Process = process;
    protected INode CurrentNode = firstNode;
    protected List<INode> CurrentBranchInstances = [firstNode];


    public BProcess GetProcess() => Process;
    public INode GetCurrent() => CurrentNode;
    public INode GetRoot() => firstNode;

    public List<INode> GetBranchTails()
    {
        List<INode> lastNodes = [];

        foreach (var branchInstance in CurrentBranchInstances)
        {
            lastNodes.AddRange(branchInstance.FetchLastNodes(branchInstance));
        }

        return lastNodes;
    }

    public void AddBranchInstance(INode node)
    {
        node.PrevSteps = firstNode.PrevSteps;
        CurrentBranchInstances.Add(node);
        //return CurrentBranchLastInstances;
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
}