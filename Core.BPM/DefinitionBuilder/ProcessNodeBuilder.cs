using Core.BPM.AggregateConditions;
using Core.BPM.Exceptions;
using Core.BPM.Interfaces;
using Core.BPM.Nodes;
using Marten;

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
            var configured = (ProcessNodeBuilder<TProcess>)configure.Invoke(new ProcessNodeBuilder<TProcess>(node, ProcessConfig));
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
        var condition = new AggregateCondition<TProcess>(predicate);
        var node = new ConditionalNode(ProcessConfig.ProcessType, condition);
        var builder = new ConditionalNodeBuilder<TProcess>(CurrentNode, process, [condition]);
        configure.Invoke(builder);
        throw new NotImplementedException();
    }

    public IProcessNodeModifiableBuilder<TProcess> Case<TAggregate>(Predicate<TAggregate> predicate, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure)
        where TAggregate : Aggregate
    {
        return CaseInternal(predicate, configure);
    }

    public IProcessNodeModifiableBuilder<TProcess> Group(Action<IGroupBuilder<TProcess>> configure)
    {
        var node = new GroupNode(ProcessConfig.ProcessType);
        var builder = new GroupNodeBuilder<TProcess>(process, CurrentNode, node);

        configure.Invoke(builder);
        builder.EndGroup();
        var s = CurrentBranchInstances.ToList();
        node.SetPrevSteps(s);
        node.AggregateConditions = _aggregateConditions;

        return Continue(new ProcessNodeBuilder<TProcess>(node, process, _aggregateConditions));
    }

    public IProcessNodeModifiableBuilder<TProcess> Case(Predicate<TProcess> predicate, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure)
    {
        return CaseInternal(predicate, configure);
    }

    public IProcessNodeModifiableBuilder<TProcess> Or(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure)
    {
        var nextBuilder = new ProcessNodeBuilder<TProcess>(rootNode, process);
        var configured = (ProcessNodeBuilder<TProcess>)configure.Invoke(nextBuilder);

        return configured;
    }


    public IProcessNodeModifiableBuilder<TProcess> Continue<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null)
    {
        return Continue(new ProcessNodeBuilder<TProcess>(new Node(typeof(TCommand), ProcessConfig.ProcessType), ProcessConfig, _aggregateConditions), configure);
    }

    public IProcessNodeModifiableBuilder<TProcess> ContinueAnyTime<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null)
    {
        return Continue(new ProcessNodeBuilder<TProcess>(new AnyTimeNode(typeof(TCommand), ProcessConfig.ProcessType), ProcessConfig, _aggregateConditions), configure);
    }

    public IProcessNodeNonModifiableBuilder<TProcess> UnlockOptional<TCommand>()
    {
        return Continue(new ProcessNodeBuilder<TProcess>(new OptionalNode(typeof(TCommand), ProcessConfig.ProcessType), ProcessConfig, _aggregateConditions));

        var node = new OptionalNode(typeof(TCommand), ProcessConfig.ProcessType);
        ProcessConfig.AddOptional(node, CurrentBranchInstances);
        ProcessConfig.AddDistinctCommand(node, CurrentBranchInstances);
        return this;
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
        var node = new AnyTimeNode(typeof(TCommand), typeof(TProcess));
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