using Core.BPM.Interfaces;
using Core.BPM.Nodes;

namespace Core.BPM.DefinitionBuilder;

public class ProcessNodeBuilder(INode rootNode, BProcess process)
    : BaseNodeDefinition(rootNode, process),
        IProcessNodeModifierBuilder<TProcess>, IProcessNodeInitialBuilder<TProcess>, IProcessScopedNodeInitialBuilder<TProcess> where TProcess : Aggregate
{
    private ProcessNodeBuilder<TProcess> Continue(ProcessNodeBuilder<TProcess> nextBuilder,
        Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        nextBuilder.CurrentBranchInstances.ForEach(x => x.SetPrevSteps(CurrentBranchInstances));
        if (configure is not null)
            return (ProcessNodeBuilder<TProcess>)configure.Invoke(nextBuilder);

        return nextBuilder;
    }

    private IProcessNodeModifierBuilder<TProcess> ThenContinue<TCommand>(INode node,
        Func<IProcessScopedNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        return null;
    }

    public IProcessNodeModifierBuilder<TProcess> Continue<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        return Continue(new ProcessNodeBuilder<TProcess>(new Node(typeof(TCommand), GetProcess().ProcessType), Process), configure);
    }

    public IProcessNodeModifierBuilder<TProcess> ContinueAnyTime<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        return Continue(new ProcessNodeBuilder<TProcess>(new AnyTimeNode(typeof(TCommand), GetProcess().ProcessType), Process), configure);
    }

    public IProcessNodeModifierBuilder<TProcess> ContinueOptional<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        return Continue(new ProcessNodeBuilder<TProcess>(new OptionalNode(typeof(TCommand), GetProcess().ProcessType), Process), configure);
    }

    public IProcessNodeModifierBuilder<TProcess> Case(Predicate<TProcess> predicate, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        throw new NotImplementedException();
    }


    public IProcessNodeModifierBuilder<TProcess> Or<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        var node = new Node(typeof(TCommand), typeof(TProcess));

        return Or(node, configure);
    }

    public IProcessNodeModifierBuilder<TProcess> OrOptional<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        var node = new OptionalNode(typeof(TCommand), typeof(TProcess));
        return Or(node, configure);
    }

    public IProcessNodeModifierBuilder<TProcess> OrAnyTime<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        var node = new OptionalNode(typeof(TCommand), typeof(TProcess));
        return Or(node, configure);
    }

    private IProcessNodeModifierBuilder<TProcess> Or(INode node, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        if (configure is not null)
        {
            var configured = (ProcessNodeBuilder<TProcess>)configure.Invoke(new ProcessNodeBuilder<TProcess>(node, Process));
            configured.CurrentBranchInstances.ForEach(AddBranchInstance);
            return this;
        }

        AddBranchInstance(node);
        //if (configuredBranchInstance is not null)
        //    AddBranchInstance(configuredBranchInstance.CurrentBranchInstances ?? node);
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

    private void ReverseGraph()
    {
        var currentNodeSet = CurrentBranchInstances;
        GoToRootSetNexts(currentNodeSet);
    }

    private void GoToRootSetNexts(List<INode> currentNodeSet)
    {
        foreach (var currentBranchInstance in currentNodeSet)
        {
            currentBranchInstance.PrevSteps?.ForEach(x => x.SetNextSteps(currentNodeSet));
            if (currentBranchInstance.PrevSteps is not null)
                GoToRootSetNexts(currentBranchInstance.PrevSteps);
        }
    }

    public MyClass<TProcess> End()
    {
        ReverseGraph();
        return new MyClass<TProcess>();
    }
}