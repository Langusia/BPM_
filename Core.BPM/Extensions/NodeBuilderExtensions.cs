using Core.BPM.Interfaces.Builder;

namespace Core.BPM.Extensions;

public static class OuterNodeBuilderExtensions
{
    public static INodeBuilderBuilder Or<TCommand>(this INodeBuilderBuilder builder, Action<INodeBuilderBuilder>? configure = null)
    {
        var p = builder.GetProcess();
        var r = builder.GetRoot();
        var n = new Node(typeof(TCommand), p.ProcessType);
        builder.SetCurrent(n);
        n.AddPrevStep(r);
        if (r.PrevSteps is not null)
            r.AddNextStep(n);

        configure?.Invoke(builder);
        return builder;
    }

    public static INodeBuilderBuilder ThenContinue<TCommand>(this INodeBuilderBuilder builder, Action<INodeBuilderBuilder>? configure = null)
    {
        var node = new Node(typeof(TCommand), builder.GetProcess().ProcessType);

        builder.GetCurrent().AddNextStepToTail(node);
        node.AddPrevStep(builder.GetCurrent());

        if (configure is not null)
        {
            var nextNodeBuilder = new NodeBuilder(node, builder.GetProcess());
            configure?.Invoke(nextNodeBuilder);
        }

        builder.SetCurrent(node);

        return builder;
    }

    public static INodeBuilderBuilder ThenAnyTime<TCommand>(this INodeBuilderBuilder builder, Action<INodeBuilderBuilder>? configure = null)
    {
        var node = new Node(typeof(TCommand), builder.GetProcess().ProcessType);
        node.AnyTime = true;
        builder.GetCurrent().AddNextStepToTail(node);
        node.AddPrevStep(builder.GetCurrent());

        if (configure is not null)
        {
            var nextNodeBuilder = new NodeBuilder(node, builder.GetProcess());
            configure?.Invoke(nextNodeBuilder);
        }

        builder.SetCurrent(node);

        return builder;
    }
}