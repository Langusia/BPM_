using Core.BPM.AggregateConditions;
using Core.BPM.Interfaces;
using Core.BPM.Nodes;
using Core.BPM.Persistence.Exceptions;

namespace Core.BPM.DefinitionBuilder;

public class ProcessNodeBuilder<TProcess>(INode rootNode, BProcess process, List<IAggregateCondition>? aggregateConditions = null)
    : BaseNodeDefinition(rootNode, process),
        IProcessNodeModifiableBuilder<TProcess>, IProcessNodeNonModifiableBuilder<TProcess>, IProcessNodeInitialBuilder<TProcess> where TProcess : Aggregate
{
    private ProcessNodeBuilder<TProcess>? _orScopeBuilder;
    private List<IAggregateCondition>? _aggregateConditions = aggregateConditions;

    private ProcessNodeBuilder<TProcess> Continue(ProcessNodeBuilder<TProcess> nextBuilder,
        Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null)
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

    private IProcessNodeModifiableBuilder<TProcess> Or(INode node, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null)
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

    private IProcessNodeModifiableBuilder<TProcess> CaseInternal<T>(Predicate<T> predicate, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure)
        where T : Aggregate
    {
        _aggregateConditions ??= [];
        _aggregateConditions.Add(new AggregateCondition<T>(predicate));
        var configured = (ProcessNodeBuilder<TProcess>)configure.Invoke(this);
        configured._aggregateConditions = null;
        return configured;
    }

    public IProcessNodeModifiableBuilder<TProcess> Case<TAggregate>(Predicate<TAggregate> predicate, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure)
        where TAggregate : Aggregate
    {
        return CaseInternal(predicate, configure);
    }

    public IProcessNodeModifiableBuilder<TProcess> Case(Predicate<TProcess> predicate, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure)
    {
        return CaseInternal(predicate, configure);
    }


    public IProcessNodeModifiableBuilder<TProcess> Continue<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null)
    {
        return Continue(new ProcessNodeBuilder<TProcess>(new Node(typeof(TCommand), GetProcess().ProcessType), Process, _aggregateConditions), configure);
    }

    public IProcessNodeModifiableBuilder<TProcess> ContinueAnyTime<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null)
    {
        return Continue(new ProcessNodeBuilder<TProcess>(new AnyTimeNode(typeof(TCommand), GetProcess().ProcessType), Process, _aggregateConditions), configure);
    }

    public IProcessNodeNonModifiableBuilder<TProcess> ContinueOptional<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null)
    {
        return Continue(new ProcessNodeBuilder<TProcess>(new OptionalNode(typeof(TCommand), GetProcess().ProcessType), Process, _aggregateConditions), configure);
    }


    public IProcessNodeModifiableBuilder<TProcess> Or<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null)
    {
        var node = new Node(typeof(TCommand), typeof(TProcess));

        return Or(node, configure);
    }

    public IProcessNodeModifiableBuilder<TProcess> OrOptional<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null)
    {
        var node = new OptionalNode(typeof(TCommand), typeof(TProcess));
        return Or(node, configure);
    }

    public IProcessNodeModifiableBuilder<TProcess> OrAnyTime<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null)
    {
        var node = new OptionalNode(typeof(TCommand), typeof(TProcess));
        return Or(node, configure);
    }


    private INode? GetAllWays()
    {
        var currentNodeSet = CurrentBranchInstances;
        List<INode> result = new List<INode>();
        List<INode> resultMandatory = new List<INode>();
        GoToRootSetNexts(currentNodeSet, result, resultMandatory);
        return result?.FirstOrDefault();
    }

    private void GoToRootSetNexts(List<INode> currentNodeSet, List<INode> res, List<INode> resMandatory)
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
                {
                    if (currentBranchInstancePrevStep.NextSteps.Select(x => x.CommandType).Contains(currentBranchInstance.CommandType))
                        throw new SameCommandFoundException(currentBranchInstance.CommandType.Name);

                    currentBranchInstancePrevStep.AddNextStep(currentBranchInstance);
                }
            }

            GoToRootSetNexts(currentBranchInstance.PrevSteps!, res, resMandatory);
        }
    }

    public MyClass<TProcess> End()
    {
        Process.RootNode = GetAllWays();
        return new MyClass<TProcess>();
    }
}