using Core.BPM.BCommand;
using Core.BPM.DefinitionBuilder;
using Core.BPM.Interfaces;
using Core.BPM.Nodes;

namespace Core.BPM.Extensions;

public static class OuterNodeBuilderExtensions
{
    public static INodeDefinitionBuilder Or<TCommand>(this INodeDefinitionBuilder builder, INode node, Action<INodeDefinitionBuilder>? configure = null)
    {
        var p = builder.GetProcess();
        var r = builder.GetRoot();

        builder.SetCurrent(node);
        node.AddPrevStep(r);
        //if (r.PrevSteps is not null)
        r.AddNextStep(node);

        configure?.Invoke(builder);
        return builder;
    }

    public static INodeDefinitionBuilder OrOptional<TCommand>(this INodeDefinitionBuilder builder, Action<INodeDefinitionBuilder>? configure = null) =>
        builder.Or<TCommand>(new OptionalNode(typeof(TCommand), builder.GetProcess().ProcessType), configure);

    public static INodeDefinitionBuilder OrAnyTime<TCommand>(this INodeDefinitionBuilder builder, Action<INodeDefinitionBuilder>? configure = null) =>
        builder.Or<TCommand>(new AnyTimeNode(typeof(TCommand), builder.GetProcess().ProcessType), configure);

    public static INodeDefinitionBuilder Or<TCommand>(this INodeDefinitionBuilder builder, Action<INodeDefinitionBuilder>? configure = null) =>
        builder.Or<TCommand>(new Node(typeof(TCommand), builder.GetProcess().ProcessType), configure);

    public static INodeDefinitionBuilder ThenContinueOptional<TCommand>(this INodeDefinitionBuilder builder, Action<INodeDefinitionBuilder>? configure = null) =>
        builder.ThenContinue<TCommand>(new OptionalNode(typeof(TCommand), builder.GetProcess().ProcessType), configure);

    public static INodeDefinitionBuilder ThenContinueAnyTime<TCommand>(this INodeDefinitionBuilder builder, Action<INodeDefinitionBuilder>? configure = null) =>
        builder.ThenContinue<TCommand>(new AnyTimeNode(typeof(TCommand), builder.GetProcess().ProcessType), configure);

    public static INodeDefinitionBuilder ThenContinue<TCommand>(this INodeDefinitionBuilder builder, Action<INodeDefinitionBuilder>? configure = null) =>
        builder.ThenContinue<TCommand>(new Node(typeof(TCommand), builder.GetProcess().ProcessType), configure);

    private static INodeDefinitionBuilder ThenContinue<TCommand>(this INodeDefinitionBuilder builder, INode node, Action<INodeDefinitionBuilder>? configure = null)
    {
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


    public static INodeDefinitionBuilder Configure(this INodeDefinitionBuilder builder, Action<BpmEventOptions> configure)
    {
        configure(builder.GetCurrent().Options);
        return builder;
    }

    public static INodeDefinitionBuilder ThenContinueWithConfiguring<TCommand>(this INodeDefinitionBuilder builder,
        Action<BpmEventOptions>? configureOptions = null,
        Action<INodeDefinitionBuilder>? configure = null)
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
}