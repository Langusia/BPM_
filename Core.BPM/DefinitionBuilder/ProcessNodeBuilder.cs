using Core.BPM.Interfaces;
using Core.BPM.Nodes;

namespace Core.BPM.DefinitionBuilder;

public class ProcessNodeBuilder<TProcess>(INode rootNode, BProcess process) : BaseNodeDefinition(rootNode, process),
    IProcessNodeModifierBuilder<TProcess>, IProcessNodeInitialBuilder<TProcess>, IProcessScopedNodeInitialBuilder<TProcess> where TProcess : Aggregate
{
    private BaseNodeDefinition? ScopedBuilderBuffer;

    public IProcessNodeModifierBuilder<TProcess> Continue<TCommand>(Action<IProcessScopedNodeInitialBuilder<TProcess>>? configure = null)
    {
        Continue(new Node(typeof(TCommand), GetProcess().ProcessType), configure);
        return this;
    }

    public IProcessNodeModifierBuilder<TProcess> ContinueAnyTime<TCommand>(Action<IProcessScopedNodeInitialBuilder<TProcess>>? configure = null)
    {
        Continue(new AnyTimeNode(typeof(TCommand), GetProcess().ProcessType), configure);
        return this;
    }

    public IProcessNodeModifierBuilder<TProcess> ContinueOptional<TCommand>(Action<IProcessScopedNodeInitialBuilder<TProcess>>? configure = null)
    {
        Continue(new OptionalNode(typeof(TCommand), GetProcess().ProcessType), configure);
        return this;
    }

    public IProcessNodeModifierBuilder<TProcess> Case(Predicate<TProcess> predicate, Action<IProcessNodeInitialBuilder<TProcess>> configure)
    {
        throw new NotImplementedException();
    }


    private void Continue(INode node, Action<IProcessScopedNodeInitialBuilder<TProcess>>? configure)
    {
        if (BuilderBuffer is not null)
        {
            MergeAndReset(BuilderBuffer);
            BuilderBuffer = new ProcessNodeBuilder<TProcess>(node, Process);
        }
        else
        {
            var s = new ProcessNodeBuilder<TProcess>(node, Process);
            BuilderBuffer = s;
            if (configure is not null)
            {
                configure?.Invoke((IProcessScopedNodeInitialBuilder<TProcess>)BuilderBuffer);
            }
        }
    }

    private IProcessNodeModifierBuilder<TProcess> ThenContinue<TCommand>(INode node, Action<IProcessScopedNodeInitialBuilder<TProcess>>? configure = null)
    {
        BuilderBuffer = this;


        if (ScopedBuilderBuffer is not null)
        {
            ScopedBuilderBuffer = this;
        }
        else
        {
            BuilderBuffer = new ProcessNodeBuilder<TProcess>(node, Process);
            if (configure is not null)
            {
                configure?.Invoke((IProcessScopedNodeInitialBuilder<TProcess>)BuilderBuffer);
            }
        }

        var lastBranchNode = CurrentBranchInstances.Last();
        lastBranchNode.AddNextStep(node);
        lastBranchNode = node;

        if (configure is not null)
        {
            configure?.Invoke(this);
        }

        return this;
    }

    public IProcessNodeModifierBuilder<TProcess> Or<TCommand>(Action<IProcessScopedNodeInitialBuilder<TProcess>>? configure = null)
    {
        var node = new Node(typeof(TCommand), typeof(TProcess));
        return Or(node, configure);
    }

    public IProcessNodeModifierBuilder<TProcess> OrOptional<TCommand>(Action<IProcessScopedNodeInitialBuilder<TProcess>>? configure = null)
    {
        var node = new OptionalNode(typeof(TCommand), typeof(TProcess));
        return Or(node, configure);
    }

    public IProcessNodeModifierBuilder<TProcess> OrAnyTime<TCommand>(Action<IProcessScopedNodeInitialBuilder<TProcess>>? configure = null)
    {
        var node = new OptionalNode(typeof(TCommand), typeof(TProcess));
        return Or(node, configure);
    }

    private IProcessNodeModifierBuilder<TProcess> Or(INode node, Action<IProcessScopedNodeInitialBuilder<TProcess>>? configure)
    {
        BuilderBuffer?.AddBranchInstance(node);

        if (configure is not null)
        {
            var nextNodeBuilder = new ProcessNodeBuilder<TProcess>(node, Process);
            configure?.Invoke(nextNodeBuilder);
            MergeAndReset(nextNodeBuilder);
        }

        return this;
    }

    public IProcessNodeModifierBuilder<TProcess> ThenContinue<TCommand>(Action<IProcessScopedNodeInitialBuilder<TProcess>>? configure = null)
    {
        ThenContinue<TCommand>(new Node(typeof(TCommand), Process.ProcessType), configure);
        return this;
    }

    public IProcessNodeModifierBuilder<TProcess> ThenContinueAnyTime<TCommand>(Action<IProcessScopedNodeInitialBuilder<TProcess>>? configure = null)
    {
        ThenContinue<TCommand>(new AnyTimeNode(typeof(TCommand), Process.ProcessType), configure);
        return this;
    }

    public IProcessNodeModifierBuilder<TProcess> ThenContinueOptional<TCommand>(Action<IProcessScopedNodeInitialBuilder<TProcess>>? configure = null)
    {
        ThenContinue<TCommand>(new OptionalNode(typeof(TCommand), Process.ProcessType), configure);
        return this;
    }


    public MyClass<TProcess> End()
    {
        End();
        return new MyClass<TProcess>();
    }
}