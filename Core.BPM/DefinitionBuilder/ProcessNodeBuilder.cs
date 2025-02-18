using Core.BPM.AggregateConditions;
using Core.BPM.DefinitionBuilder.Interfaces;
using Core.BPM.Evaluators.Factory;
using Core.BPM.Interfaces;
using Core.BPM.Nodes;

namespace Core.BPM.DefinitionBuilder;

public class ProcessNodeBuilder<TProcess>(INode rootNode, BProcess process, INodeEvaluatorFactory nodeEvaluatorFactory, List<IAggregateCondition>? aggregateConditions = null)
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
            var configured = (ProcessNodeBuilder<TProcess>)configure.Invoke(new ProcessNodeBuilder<TProcess>(node, ProcessConfig, nodeEvaluatorFactory));
            if (_orScopeBuilder is null)
            {
                node.SetPrevSteps(GetRoot().PrevSteps);

                process.AddDistinctCommand(node, GetRoot().PrevSteps);
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

    public IConditionalModifiableBuilder<TProcess> If(Predicate<TProcess> predicate, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure)
    {
        return If<TProcess>(predicate, configure);
    }

    public IConditionalModifiableBuilder<TProcess> If<T>(Predicate<T> predicate, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure) where T : Aggregate
    {
        var b = new ProcessNodeBuilder<TProcess>(null, process, nodeEvaluatorFactory);
        var configedBranch = (configure.Invoke(b));
        var ifNodes = ((BaseNodeDefinition)configedBranch).CurrentBranchInstances;

        var condition = new AggregateCondition<T>(predicate);
        var conditionalNode = new ConditionalNode(ProcessConfig.ProcessType, condition, nodeEvaluatorFactory);
        var cBldr = new ConditionalNodeBuilder<TProcess>(conditionalNode, process, conditionalNode, ifNodes, configedBranch, nodeEvaluatorFactory);
        cBldr.SetIfNode(ifNodes);
        conditionalNode.SetPrevSteps(CurrentBranchInstances.ToList());
        conditionalNode.AggregateConditions = _aggregateConditions;
        //return Continue(new ProcessNodeBuilder<TProcess>(conditionalNode, process, nodeEvaluatorFactory));
        return cBldr;
    }

    public IProcessNodeModifiableBuilder<TProcess> Case<TAggregate>(Predicate<TAggregate> predicate, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure)
        where TAggregate : Aggregate
    {
        return CaseInternal(predicate, configure);
    }

    public IProcessNodeModifiableBuilder<TProcess> Group(Action<IGroupBuilder<TProcess>> configure)
    {
        var groupNode = new GroupNode(ProcessConfig.ProcessType, nodeEvaluatorFactory);
        var builder = new GroupNodeBuilder<TProcess>(process, CurrentNode, groupNode, nodeEvaluatorFactory);

        configure.Invoke(builder);
        builder.EndGroup();
        groupNode.SetPrevSteps(CurrentBranchInstances.ToList());
        groupNode.AggregateConditions = _aggregateConditions;

        return Continue(new ProcessNodeBuilder<TProcess>(groupNode, process, nodeEvaluatorFactory, _aggregateConditions));
    }

    public IProcessNodeModifiableBuilder<TProcess> Case(Predicate<TProcess> predicate, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure)
    {
        return CaseInternal(predicate, configure);
    }

    public IProcessNodeModifiableBuilder<TProcess> Or(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure)
    {
        var nextBuilder = new ProcessNodeBuilder<TProcess>(rootNode, process, nodeEvaluatorFactory);
        var configured = (ProcessNodeBuilder<TProcess>)configure.Invoke(nextBuilder);

        return configured;
    }


    public IProcessNodeModifiableBuilder<TProcess> Continue<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null)
    {
        return Continue(new ProcessNodeBuilder<TProcess>(new Node(typeof(TCommand), ProcessConfig.ProcessType, nodeEvaluatorFactory), ProcessConfig, nodeEvaluatorFactory, _aggregateConditions),
            configure);
    }

    public IProcessNodeModifiableBuilder<TProcess> ContinueAnyTime<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null)
    {
        return Continue(new ProcessNodeBuilder<TProcess>(new AnyTimeNode(typeof(TCommand), ProcessConfig.ProcessType, nodeEvaluatorFactory), ProcessConfig, nodeEvaluatorFactory, _aggregateConditions),
            configure);
    }

    public IProcessNodeNonModifiableBuilder<TProcess> UnlockOptional<TCommand>()
    {
        return Continue(
            new ProcessNodeBuilder<TProcess>(new OptionalNode(typeof(TCommand), ProcessConfig.ProcessType, nodeEvaluatorFactory), ProcessConfig, nodeEvaluatorFactory, _aggregateConditions));
    }


    public IProcessNodeModifiableBuilder<TProcess> Or<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null)
    {
        var node = new Node(typeof(TCommand), typeof(TProcess), nodeEvaluatorFactory);

        return Or(node, configure);
    }


    public IProcessNodeModifiableBuilder<TProcess> OrOptional<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null)
    {
        var node = new OptionalNode(typeof(TCommand), typeof(TProcess), nodeEvaluatorFactory);
        return Or(node, configure);
    }

    public IProcessNodeModifiableBuilder<TProcess> OrAnyTime<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null)
    {
        var node = new AnyTimeNode(typeof(TCommand), typeof(TProcess), nodeEvaluatorFactory);
        return Or(node, configure);
    }

    public ProcessConfig<TProcess> End(Action<BProcessConfig>? configureProcess)
    {
        var res = GetConfiguredProcessRootReverse();
        ProcessConfig.RootNode = res.Item1;
        ProcessConfig.AllDistinctCommands = res.Item2;
        var allPossibles = GetAllPossibles(ProcessConfig.RootNode);

        ProcessConfig.AllPossibles = allPossibles;
        var s = process.AllNodes;
        var ss = process.AllNodes.Distinct().ToList();
        if (configureProcess is not null)
            configureProcess(process.Config);
        return new ProcessConfig<TProcess>();
    }
}