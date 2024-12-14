using Core.BPM.AggregateConditions;
using Core.BPM.Interfaces;
using Core.BPM.Nodes;

namespace Core.BPM.DefinitionBuilder;

public class ProcessNodeBuilder<TProcess>(INode rootNode, BProcess process, List<IAggregateCondition>? aggregateConditions = null)
    : BaseNodeDefinition(rootNode, process),
        IProcessNodeModifierBuilder<TProcess>, IProcessNodeInitialBuilder<TProcess> where TProcess : Aggregate
{
    private ProcessNodeBuilder<TProcess>? _orScopeBuilder;
    private List<IAggregateCondition>? _aggregateConditions = aggregateConditions;

    private ProcessNodeBuilder<TProcess> Continue(ProcessNodeBuilder<TProcess> nextBuilder,
        Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        nextBuilder.CurrentBranchInstances.ForEach(x =>
        {
            x.SetPrevSteps(CurrentBranchInstances);
            x.AggregateConditions = _aggregateConditions;
        });
        if (configure is not null)
        {
            var configuredBuilder = (ProcessNodeBuilder<TProcess>)configure.Invoke(nextBuilder);
            configuredBuilder._orScopeBuilder = this;
            return configuredBuilder;
        }

        return nextBuilder;
    }

    private IProcessNodeModifierBuilder<TProcess> Or(INode node, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        if (configure is not null)
        {
            var configured = (ProcessNodeBuilder<TProcess>)configure.Invoke(new ProcessNodeBuilder<TProcess>(node, Process));
            if (_orScopeBuilder is null)
            {
                node.SetPrevSteps(GetRoot().PrevSteps);
            }
            else
            {
                node.SetPrevSteps(_orScopeBuilder.CurrentBranchInstances);
            }

            node.AggregateConditions = _aggregateConditions;
            CurrentBranchInstances.AddRange(configured.CurrentBranchInstances);
            return this;
        }

        if (_orScopeBuilder is null)
            node.SetPrevSteps(GetRoot().PrevSteps);
        else
        {
            node.SetPrevSteps(_orScopeBuilder.CurrentBranchInstances);
        }

        node.AggregateConditions = _aggregateConditions;
        CurrentBranchInstances.Add(node);
        return this;
    }

    private IProcessNodeModifierBuilder<TProcess> CaseInternal<T>(Predicate<T> predicate, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>> configure)
        where T : Aggregate
    {
        _aggregateConditions ??= [];
        _aggregateConditions.Add(new AggregateCondition<T>(predicate));
        var configured = (ProcessNodeBuilder<TProcess>)configure.Invoke(this);
        configured._aggregateConditions = null;
        return configured;
    }

    public IProcessNodeModifierBuilder<TProcess> Case<TAggregate>(Predicate<TAggregate> predicate, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>> configure)
        where TAggregate : Aggregate
    {
        return CaseInternal(predicate, configure);
    }

    public IProcessNodeModifierBuilder<TProcess> Case(Predicate<TProcess> predicate, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>> configure)
    {
        return CaseInternal(predicate, configure);
    }


    public IProcessNodeModifierBuilder<TProcess> Continue<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        return Continue(new ProcessNodeBuilder<TProcess>(new Node(typeof(TCommand), GetProcess().ProcessType), Process, _aggregateConditions), configure);
    }

    public IProcessNodeModifierBuilder<TProcess> ContinueAnyTime<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        return Continue(new ProcessNodeBuilder<TProcess>(new AnyTimeNode(typeof(TCommand), GetProcess().ProcessType), Process, _aggregateConditions), configure);
    }

    public IProcessNodeModifierBuilder<TProcess> ContinueOptional<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    {
        return Continue(new ProcessNodeBuilder<TProcess>(new OptionalNode(typeof(TCommand), GetProcess().ProcessType), Process, _aggregateConditions), configure);
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