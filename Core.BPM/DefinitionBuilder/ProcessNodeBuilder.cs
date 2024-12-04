using Core.BPM.Interfaces;
using Core.BPM.Nodes;

namespace Core.BPM.DefinitionBuilder;

public class ProcessNodeBuilder<TProcess>(INode rootNode, BProcess process, ProcessNodeBuilder<TProcess>? PrevBuilder) : BaseNodeDefinition(rootNode, process),
    IProcessNodeModifierBuilder<TProcess>, IProcessNodeInitialBuilder<TProcess>, IProcessScopedNodeInitialBuilder<TProcess> where TProcess : Aggregate
{
    public void MergePreviousToCurrentBranches(ProcessNodeBuilder<TProcess> bb)
    {
        foreach (var currentBranchInstance in CurrentBranchInstances)
        {
            bb.CurrentBranchInstances.Add(currentBranchInstance);
        }
    }

    public void BindPrevs()
    {
        foreach (var currentBranchInstance in CurrentBranchInstances)
        {
            currentBranchInstance.AddPrevSteps(PrevBuilder?.CurrentBranchInstances);
        }
    }

    public void BindNexts(ProcessNodeBuilder<TProcess> builder)
    {
        foreach (var currentBranchInstance in builder.CurrentBranchInstances)
        {
            currentBranchInstance.AddNextSteps(CurrentBranchInstances);
        }
    }

    public void MergeNextStepsToCurrentBranches(ProcessNodeBuilder<TProcess> bb)
    {
        //foreach (var currentBranchInstance in OriginBranchInstances)
        //{
        //    currentBranchInstance.AddNextSteps(bb.CurrentBranchInstances);
        //}
    }

    public void MergePreviousToRootScope()
    {
        foreach (var currentBranchInstance in CurrentBranchInstances)
        {
            currentBranchInstance.AddPrevStep(rootNode);
        }
    }

    private ProcessNodeBuilder<TProcess> Continue(INode node, Func<IProcessScopedNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        if (configure is not null)
        {
            var configuredBranch = configure.Invoke(new ProcessNodeBuilder<TProcess>(rootNode, process, this));
            //((ProcessNodeBuilder<TProcess>)configuredBranch).MergePreviousToCurrentBranches(configuredBranch);
            return (ProcessNodeBuilder<TProcess>)configuredBranch;
        }

        if (PrevBuilder is null)
        {
            var s = new ProcessNodeBuilder<TProcess>(node, Process, this);
            Merge(s);
            return s;
        }
        
        var newBuilder = new ProcessNodeBuilder<TProcess>(node, Process, this);
        if (!Lock)
        {
            Lock = true;
            newBuilder.BindPrevs();
            return this;
        }

        BindNexts(newBuilder);
        return newBuilder;
    }

    public IProcessNodeModifierBuilder<TProcess> Continue<TCommand>(Func<IProcessScopedNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        return Continue(new Node(typeof(TCommand), GetProcess().ProcessType), configure);
    }

    public IProcessNodeModifierBuilder<TProcess> ContinueAnyTime<TCommand>(Func<IProcessScopedNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        return Continue(new AnyTimeNode(typeof(TCommand), GetProcess().ProcessType), configure);
    }

    public IProcessNodeModifierBuilder<TProcess> ContinueOptional<TCommand>(Func<IProcessScopedNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        return Continue(new OptionalNode(typeof(TCommand), GetProcess().ProcessType), configure);
    }

    public IProcessNodeModifierBuilder<TProcess> Case(Predicate<TProcess> predicate, Func<IProcessScopedNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        throw new NotImplementedException();
    }


    private IProcessNodeModifierBuilder<TProcess> ThenContinue<TCommand>(INode node,
        Func<IProcessScopedNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        var nextBuilder = new ProcessNodeBuilder<TProcess>(rootNode, Process, this);
        nextBuilder.MergePreviousToRootScope();

        return this;
    }

    public IProcessNodeModifierBuilder<TProcess> Or<TCommand>(Func<IProcessScopedNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        var node = new Node(typeof(TCommand), typeof(TProcess));
        return Or(node, configure);
    }

    public IProcessNodeModifierBuilder<TProcess> OrOptional<TCommand>(Func<IProcessScopedNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        var node = new OptionalNode(typeof(TCommand), typeof(TProcess));
        return Or(node, configure);
    }

    public IProcessNodeModifierBuilder<TProcess> OrAnyTime<TCommand>(Func<IProcessScopedNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        var node = new OptionalNode(typeof(TCommand), typeof(TProcess));
        return Or(node, configure);
    }

    private IProcessNodeModifierBuilder<TProcess> Or(INode node, Func<IProcessScopedNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        AddBranchInstance(node);

        //if (configure is not null)
        //{
        //    var nextNodeBuilder = new ProcessNodeBuilder<TProcess>(node, Process);
        //    configure?.Invoke(nextNodeBuilder);
        //    MergeAndReset(nextNodeBuilder);
        //}

        return this;
    }

    public IProcessNodeModifierBuilder<TProcess> ThenContinue<TCommand>(Func<IProcessScopedNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure)
    {
        var newNode = new Node(typeof(TCommand), Process.ProcessType);
        return ThenContinue<TCommand>(newNode, configure);
    }

    public IProcessNodeModifierBuilder<TProcess> ThenContinueAnyTime<TCommand>(Func<IProcessScopedNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure)
    {
        var newNode = new AnyTimeNode(typeof(TCommand), Process.ProcessType);
        return ThenContinue<TCommand>(newNode, configure);
    }

    public IProcessNodeModifierBuilder<TProcess> ThenContinueOptional<TCommand>(Func<IProcessScopedNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure)
    {
        var newNode = new OptionalNode(typeof(TCommand), Process.ProcessType);
        return ThenContinue<TCommand>(newNode, configure);
    }


    public MyClass<TProcess> End()
    {
        if (PrevBuilder is not null)
            Merge(PrevBuilder);
        return new MyClass<TProcess>();
    }
}