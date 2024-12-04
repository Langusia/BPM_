using Core.BPM.Interfaces;

namespace Core.BPM.DefinitionBuilder;

public class BaseNodeDefinition(INode rootNode, BProcess process)
{
    protected readonly BProcess Process = process;
    protected INode CurrentNode = rootNode;
    protected List<INode> CurrentBranchInstances = [rootNode];
    protected BaseNodeDefinition? BuilderBuffer;

    private INode _rootNode = rootNode;

    public BProcess GetProcess() => Process;
    public INode GetCurrent() => CurrentNode;
    public INode GetRoot() => _rootNode;
    public List<INode> GetBranchInstances() => CurrentBranchInstances;

    public List<INode> AddBranchInstance(INode node)
    {
        CurrentBranchInstances.Add(node);
        return CurrentBranchInstances;
    }

    public INode SetRoot(INode node)
    {
        _rootNode = node;
        return _rootNode;
    }

    public INode SetCurrent(INode node)
    {
        CurrentNode = node;
        return CurrentNode;
    }

    protected void End()
    {
        if (BuilderBuffer is not null)
            MergeAndReset(BuilderBuffer);
    }

    protected void MergeAndReset(BaseNodeDefinition builder)
    {
        var nextBranchInstances = builder.GetBranchInstances();
        foreach (var nextBuilderBranchInstance in nextBranchInstances)
        {
            nextBuilderBranchInstance.AddPrevSteps(CurrentBranchInstances);
        }

        foreach (var currentBranchInstance in CurrentBranchInstances)
        {
            currentBranchInstance.AddNextSteps(CurrentBranchInstances);
        }

        CurrentBranchInstances = builder.GetBranchInstances();
        BuilderBuffer = null;
    }

    protected void MergeAndReset(BaseNodeDefinition fromBuilder, BaseNodeDefinition toBuilder)
    {
        
    }
}