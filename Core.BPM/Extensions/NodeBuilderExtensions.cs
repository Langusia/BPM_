using Core.BPM.Interfaces.Builder;

namespace Core.BPM.Extensions;

public static class NodeBuilderExtensions
{
    public static IExtendableNodeBuilder Or1<TCommand>(this IExtendableNodeBuilder builder, Action<INodeBuilder>? configure = null)
    {
        var p = builder.GetProcess();
        var r = builder.GetRoot();
        var n = new Node(typeof(TCommand), p.ProcessType);
        //add prev to new
        n.AddPrevStep(r);
        //add next to root
        if (r.PrevSteps is not null)
            r.PrevSteps.ForEach(x => x.NextSteps.ForEach(z => z.AddNextStep(n)));
        else
            r.AddNextStep(n);
        configure?.Invoke(builder);
        return builder;
    }

    public static IExtendableNodeBuilder Or2<TCommand>(this IExtendableNodeBuilder builder, Action<INodeBuilder>? configure = null)
    {
        var p = builder.GetProcess();
        var r = builder.GetRoot();
        var n = new Node(typeof(TCommand), p.ProcessType);
        //add prev to new
        n.AddPrevStep(r);
        //add next to root
        if (r.PrevSteps is not null)
            r.AddNextStep(n);
        configure?.Invoke(builder);
        return builder;
    }
}