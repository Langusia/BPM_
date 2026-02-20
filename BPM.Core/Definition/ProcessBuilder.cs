using System;
using System.Linq;
using BPM.Core.Definition.Conditions;
using BPM.Core.Definition.Interfaces;
using BPM.Core.Nodes;
using BPM.Core.Nodes.Evaluation;
using BPM.Core.Process;
using MediatR;

namespace BPM.Core.Definition;

public class ProcessBuilder<TProcess>(INode rootNode, BProcess process, INodeEvaluatorFactory nodeEvaluatorFactory, int depthCounter = 0)
    : NodeBuilderBase(rootNode, process),
        IProcessNodeModifiableBuilder<TProcess>, IProcessNodeNonModifiableBuilder<TProcess>, IProcessNodeInitialBuilder<TProcess> where TProcess : Aggregate
{
    private ProcessBuilder<TProcess>? _orScopeBuilder;

    private ProcessBuilder<TProcess> Continue(ProcessBuilder<TProcess> nextBuilder,
        Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null)
    {
        nextBuilder.CurrentBranchInstances.ForEach(x => { x.SetPrevSteps(CurrentBranchInstances); });
        if (configure is not null)
        {
            var configuredBuilder = (ProcessBuilder<TProcess>)configure.Invoke(nextBuilder);
            configuredBuilder._orScopeBuilder = this;
            return configuredBuilder;
        }

        return nextBuilder;
    }

    private IProcessNodeModifiableBuilder<TProcess> Or(INode node, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null)
    {
        if (configure is not null)
        {
            var configured = (ProcessBuilder<TProcess>)configure.Invoke(new ProcessBuilder<TProcess>(node, ProcessConfig, nodeEvaluatorFactory, ++depthCounter));
            if (_orScopeBuilder is null)
            {
                node.SetPrevSteps(GetRoot().PrevSteps);
            }
            else
            {
                node.SetPrevSteps(_orScopeBuilder.CurrentBranchInstances);
            }

            CurrentBranchInstances.AddRange(configured.CurrentBranchInstances);
            return this;
        }

        if (_orScopeBuilder is null)
            node.SetPrevSteps(GetRoot().PrevSteps);
        else
        {
            node.SetPrevSteps(_orScopeBuilder.CurrentBranchInstances);
        }


        CurrentBranchInstances.Add(node);
        return this;
    }

    private IProcessNodeModifiableBuilder<TProcess> CaseInternal<T>(Predicate<T> predicate, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure)
        where T : Aggregate
    {
        var configured = (ProcessBuilder<TProcess>)configure.Invoke(this);
        return configured;
    }

    public IProcessNodeModifiableBuilder<TProcess> JumpTo<TGuestAggregate>(bool sealedSteps) where TGuestAggregate : Aggregate
    {
        var processNode = new GuestProcessNode(typeof(TGuestAggregate), sealedSteps, ProcessConfig.ProcessType, nodeEvaluatorFactory);

        return Continue(new ProcessBuilder<TProcess>(processNode, ProcessConfig, nodeEvaluatorFactory, ++depthCounter));
    }

    public IConditionalModifiableBuilder<TProcess> If(Predicate<TProcess> predicate, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure)
    {
        return If<TProcess>(predicate, configure);
    }

    public IConditionalModifiableBuilder<TProcess> If<T>(Predicate<T> predicate, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure) where T : Aggregate
    {
        var b = new ProcessBuilder<TProcess>(null, ProcessConfig, nodeEvaluatorFactory);
        var configuredBranch = configure.Invoke(b);
        return IfCore<T>(predicate, (NodeBuilderBase)configuredBranch);
    }

    public IConditionalModifiableBuilder<TProcess> If(Predicate<TProcess> predicate, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeNonModifiableBuilder<TProcess>> configure)
    {
        return If<TProcess>(predicate, configure);
    }

    public IConditionalModifiableBuilder<TProcess> If<T>(Predicate<T> predicate, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeNonModifiableBuilder<TProcess>> configure) where T : Aggregate
    {
        var b = new ProcessBuilder<TProcess>(null, ProcessConfig, nodeEvaluatorFactory);
        var configuredBranch = configure.Invoke(b);
        return IfCore<T>(predicate, (NodeBuilderBase)configuredBranch);
    }

    private IConditionalModifiableBuilder<TProcess> IfCore<T>(Predicate<T> predicate, NodeBuilderBase configuredBranch) where T : Aggregate
    {
        var ifNodes = configuredBranch.CurrentBranchInstances;

        var condition = new AggregateCondition<T>(predicate);
        var conditionalNode = new ConditionalNode(ProcessConfig.ProcessType, condition, nodeEvaluatorFactory);
        var cBldr = new ConditionalBuilder<TProcess>(conditionalNode, ProcessConfig, conditionalNode, nodeEvaluatorFactory, ++depthCounter);
        cBldr.SetIfNode(ifNodes);
        conditionalNode.SetPrevSteps(CurrentBranchInstances.ToList());
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
        var builder = new GroupNodeBuilder<TProcess>(ProcessConfig, CurrentNode, groupNode, nodeEvaluatorFactory, ++depthCounter);

        configure.Invoke(builder);
        builder.EndGroup();

        groupNode.SetPrevSteps(CurrentBranchInstances.ToList());

        return Continue(new ProcessBuilder<TProcess>(groupNode, ProcessConfig, nodeEvaluatorFactory, ++depthCounter));
    }

    public IProcessNodeModifiableBuilder<TProcess> Case(Predicate<TProcess> predicate, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure)
    {
        return CaseInternal(predicate, configure);
    }

    public IProcessNodeModifiableBuilder<TProcess> Or(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure)
    {
        var nextBuilder = new ProcessBuilder<TProcess>(CurrentNode, ProcessConfig, nodeEvaluatorFactory);
        var configured = (ProcessBuilder<TProcess>)configure.Invoke(nextBuilder);

        return configured;
    }


    public IProcessNodeModifiableBuilder<TProcess> Continue<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null)
        where TCommand : IBaseRequest
    {
        return Continue(
            new ProcessBuilder<TProcess>(new Node(typeof(TCommand), ProcessConfig.ProcessType, nodeEvaluatorFactory), ProcessConfig, nodeEvaluatorFactory, ++depthCounter),
            configure);
    }

    public IProcessNodeModifiableBuilder<TProcess> ContinueAnyTime<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null)
        where TCommand : IBaseRequest
    {
        return Continue(
            new ProcessBuilder<TProcess>(new AnyTimeNode(typeof(TCommand), ProcessConfig.ProcessType, nodeEvaluatorFactory), ProcessConfig, nodeEvaluatorFactory, ++depthCounter),
            configure);
    }

    public IProcessNodeNonModifiableBuilder<TProcess> UnlockOptional<TCommand>() where TCommand : IBaseRequest
    {
        return Continue(
            new ProcessBuilder<TProcess>(new OptionalNode(typeof(TCommand), ProcessConfig.ProcessType, nodeEvaluatorFactory), ProcessConfig, nodeEvaluatorFactory, ++depthCounter));
    }


    public IProcessNodeModifiableBuilder<TProcess> Or<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null) where TCommand : IBaseRequest
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
        where TCommand : IBaseRequest
    {
        var node = new AnyTimeNode(typeof(TCommand), typeof(TProcess), nodeEvaluatorFactory);
        return Or(node, configure);
    }

    public IProcessNodeModifiableBuilder<TProcess> OrJumpTo<TAggregate>(bool sealedSteps = true) where TAggregate : Aggregate
    {
        var processNode = new GuestProcessNode(typeof(TAggregate), sealedSteps, ProcessConfig.ProcessType, nodeEvaluatorFactory);

        return Or(processNode);
    }

    public ProcessConfig<TProcess> End(Action<BProcessConfig>? configureProcess)
    {
        var res = GetConfiguredProcessRootReverse();
        ProcessConfig.RootNode = res;
        if (configureProcess is not null)
            configureProcess(ProcessConfig.Config);

        return new ProcessConfig<TProcess>(ProcessConfig);
    }
}
