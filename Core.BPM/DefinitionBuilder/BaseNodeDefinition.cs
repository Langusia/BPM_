using Core.BPM.Interfaces;

namespace Core.BPM.DefinitionBuilder;

public class BaseNodeDefinition(INode rootNode, BProcess process) : INodeDefinitionBuilder
{
    protected readonly BProcess Process = process;
    protected INode RootNode = rootNode;
    protected INode CurrentNode = rootNode;
    protected List<INode> CurrentBranchInstances = [];

    public BProcess GetProcess() => Process;
    public INode GetCurrent() => CurrentNode;
    public INode GetRoot() => RootNode;

    public INode SetRoot(INode node)
    {
        RootNode = node;
        return RootNode;
    }

    public INode SetCurrent(INode node)
    {
        CurrentNode = node;
        return CurrentNode;
    }

    protected void Merge(INodeDefinitionBuilder builder)
    {
        foreach (var currentBranchInstance in CurrentBranchInstances)
        {
            currentBranchInstance.AddNextStep(builder.GetRoot());
        }
    }
}