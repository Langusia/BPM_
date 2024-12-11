using Core.BPM.Interfaces;
using Core.BPM.Nodes;

namespace Core.BPM.DefinitionBuilder;

public class ProcessNodeBuilder<TProcess>(INode rootNode, BProcess process, ProcessNodeBuilder<TProcess>? nb)
    : BaseNodeDefinition(rootNode, process),
        IProcessNodeModifierBuilder<TProcess>, IProcessNodeInitialBuilder<TProcess>, IProcessScopedNodeInitialBuilder<TProcess> where TProcess : Aggregate
{
    public ProcessNodeBuilder<TProcess>? _nb = nb;

    //public Stack<ProcessNodeBuilder<TProcess>?> _ns = ns;
    public ProcessNodeBuilder<TProcess>? OrScopeBuilder;

    private ProcessNodeBuilder<TProcess> Continue(ProcessNodeBuilder<TProcess> nextBuilder,
        Func<IProcessScopedNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        nextBuilder.CurrentBranchInstances.ForEach(x => x.SetPrevSteps(CurrentBranchInstances));
        //_ns.Push(this);

        _nb = this;
        if (configure is not null)
        {
            var configuredBuilder = (ProcessNodeBuilder<TProcess>)configure.Invoke(nextBuilder);
            configuredBuilder.OrScopeBuilder = this;
            return configuredBuilder;
        }

        return nextBuilder;
    }

    private ProcessNodeBuilder<TProcess> ThenContinue(ProcessNodeBuilder<TProcess> nextBuilder,
        Func<IProcessScopedNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        nextBuilder.CurrentBranchInstances.ForEach(x => x.SetPrevSteps(CurrentBranchInstances));
        //_ns.Push(this);

        if (configure is not null)
        {
            var configuredBuilder = (ProcessNodeBuilder<TProcess>)configure.Invoke(nextBuilder);
            configuredBuilder.OrScopeBuilder = this;

            //configuredBuilder.SetRoot(nextBuilder.GetRoot());
            return configuredBuilder;
        }

        return nextBuilder;
    }

    private IProcessNodeModifierBuilder<TProcess> Or(INode node, Func<IProcessScopedNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        ProcessNodeBuilder<TProcess>? configuredBranchInstance;
        if (configure is not null)
        {
            var configured = (ProcessNodeBuilder<TProcess>)configure.Invoke(new ProcessNodeBuilder<TProcess>(node, Process, null));
            configured.CurrentBranchInstances.ForEach(AddBranchInstance);
            return configured;
        }

        //AddBranchInstance(node);
        //_nb ??= _ns.Pop();
        ////_nb.AddBranchInstance(node);
        if (OrScopeBuilder is null)
            node.SetPrevSteps(GetRoot().PrevSteps);
        else
        {
            node.SetPrevSteps(OrScopeBuilder.CurrentBranchInstances);
        }

        CurrentBranchInstances.Add(node);


        //if (configuredBranchInstance is not null)
        //    AddBranchInstance(configuredBranchInstance.CurrentBranchInstances ?? node);
        return this;
    }

    public IProcessNodeModifierBuilder<TProcess> Continue<TCommand>(Func<IProcessScopedNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        return Continue(new ProcessNodeBuilder<TProcess>(new Node(typeof(TCommand), GetProcess().ProcessType), Process, this), configure);
    }

    public IProcessNodeModifierBuilder<TProcess> ContinueAnyTime<TCommand>(Func<IProcessScopedNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        return Continue(new ProcessNodeBuilder<TProcess>(new AnyTimeNode(typeof(TCommand), GetProcess().ProcessType), Process, this), configure);
    }

    public IProcessNodeModifierBuilder<TProcess> ContinueOptional<TCommand>(Func<IProcessScopedNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        return Continue(new ProcessNodeBuilder<TProcess>(new OptionalNode(typeof(TCommand), GetProcess().ProcessType), Process, this), configure);
    }

    public IProcessNodeModifierBuilder<TProcess> Case(Predicate<TProcess> predicate, Func<IProcessScopedNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        throw new NotImplementedException();
    }


    public IProcessNodeModifierBuilder<TProcess> Or<TCommand>(Func<IProcessScopedNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        var s = CurrentBranchInstances;

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


    public IProcessNodeModifierBuilder<TProcess> ThenContinue<TCommand>(Func<IProcessScopedNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure)
    {
        return ThenContinue(new ProcessNodeBuilder<TProcess>(new Node(typeof(TCommand), GetProcess().ProcessType), Process, _nb), configure);
    }

    public IProcessNodeModifierBuilder<TProcess> ThenContinueAnyTime<TCommand>(Func<IProcessScopedNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure)
    {
        return ThenContinue(new ProcessNodeBuilder<TProcess>(new AnyTimeNode(typeof(TCommand), Process.ProcessType), Process, _nb), configure);
    }

    public IProcessNodeModifierBuilder<TProcess> ThenContinueOptional<TCommand>(Func<IProcessScopedNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure)
    {
        return ThenContinue(new ProcessNodeBuilder<TProcess>(new OptionalNode(typeof(TCommand), Process.ProcessType), Process, _nb), configure);
    }


    private INode ReverseGraph()
    {
        var currentNodeSet = CurrentBranchInstances;

        var s = GoToRootSetNexts(currentNodeSet);
        return s;
    }

    private INode GoToRootSetNexts(List<INode> currentNodeSet)
    {
        INode node = null;
        foreach (var currentBranchInstance in currentNodeSet)
        {
            if (currentBranchInstance.PrevSteps is null)
            {
                node = currentBranchInstance;
                continue;
            }

            currentBranchInstance.PrevSteps?.ForEach(x => x.AddNextStep(currentBranchInstance));
            GoToRootSetNexts(currentBranchInstance.PrevSteps!);
        }

        return node;
    }

    public MyClass<TProcess> End()
    {
        var s = CurrentBranchInstances;

        //Process.RootNode = ReverseGraph();
        return new MyClass<TProcess>();
    }
}