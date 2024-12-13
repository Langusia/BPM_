using Core.BPM.Interfaces;
using Core.BPM.Nodes;

namespace Core.BPM.DefinitionBuilder;

public class ProcessNodeBuilder<TProcess>(INode rootNode, BProcess process, ProcessNodeBuilder<TProcess>? nb)
    : BaseNodeDefinition(rootNode, process),
        IProcessNodeModifierBuilder<TProcess>, IProcessNodeInitialBuilder<TProcess>, IProcessScopedNodeInitialBuilder<TProcess> where TProcess : Aggregate
{
    public ProcessNodeBuilder<TProcess>? _nb = nb;
    public ProcessNodeBuilder<TProcess>? OrScopeBuilder;

    private ProcessNodeBuilder<TProcess> Continue(ProcessNodeBuilder<TProcess> nextBuilder,
        Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        nextBuilder.CurrentBranchInstances.ForEach(x => x.SetPrevSteps(CurrentBranchInstances));
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
        if (configure is not null)
        {
            var configuredBuilder = (ProcessNodeBuilder<TProcess>)configure.Invoke(nextBuilder);
            configuredBuilder.OrScopeBuilder = this;

            return configuredBuilder;
        }

        return nextBuilder;
    }

    private IProcessNodeModifierBuilder<TProcess> Or(INode node, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        if (configure is not null)
        {
            var configured = (ProcessNodeBuilder<TProcess>)configure.Invoke(new ProcessNodeBuilder<TProcess>(node, Process, null));
            if (OrScopeBuilder is null)
                node.SetPrevSteps(GetRoot().PrevSteps);
            else
            {
                node.SetPrevSteps(OrScopeBuilder.CurrentBranchInstances);
            }

            CurrentBranchInstances.AddRange(configured.CurrentBranchInstances);
            return this;
        }

        if (OrScopeBuilder is null)
            node.SetPrevSteps(GetRoot().PrevSteps);
        else
        {
            node.SetPrevSteps(OrScopeBuilder.CurrentBranchInstances);
        }

        CurrentBranchInstances.Add(node);
        return this;
    }

    public IProcessNodeModifierBuilder<TProcess> Case<TAggregate>(Predicate<TAggregate> predicate, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>> configure) where TAggregate : Aggregate
    {
        throw new NotImplementedException();
    }

    public IProcessNodeModifierBuilder<TProcess> Continue<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        return Continue(new ProcessNodeBuilder<TProcess>(new Node(typeof(TCommand), GetProcess().ProcessType), Process, this), configure);
    }

    public IProcessNodeModifierBuilder<TProcess> ContinueAnyTime<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        return Continue(new ProcessNodeBuilder<TProcess>(new AnyTimeNode(typeof(TCommand), GetProcess().ProcessType), Process, this), configure);
    }

    public IProcessNodeModifierBuilder<TProcess> ContinueOptional<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        return Continue(new ProcessNodeBuilder<TProcess>(new OptionalNode(typeof(TCommand), GetProcess().ProcessType), Process, this), configure);
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


    private INode? GetAllWays()
    {
        var currentNodeSet = CurrentBranchInstances;
        List<INode> result = new List<INode>();
        GoToRootSetNexts(currentNodeSet, result);
        return result?.FirstOrDefault();
    }

    private void GoToRootSetNexts(List<INode> currentNodeSet, List<INode> res)
    {
        foreach (var currentBranchInstance in currentNodeSet)
        {
            if (currentBranchInstance.PrevSteps is null)
            {
                res.Add(currentBranchInstance);
                continue;
            }

            foreach (var currentBranchInstancePrevStep in currentBranchInstance.PrevSteps)
            {
                if (!currentBranchInstancePrevStep.NextSteps!.Contains(currentBranchInstance))
                    currentBranchInstancePrevStep.AddNextStep(currentBranchInstance);
            }

            GoToRootSetNexts(currentBranchInstance.PrevSteps!, res);
        }
    }

    public MyClass<TProcess> End()
    {
        Process.RootNode = GetAllWays();
        return new MyClass<TProcess>();
    }
}