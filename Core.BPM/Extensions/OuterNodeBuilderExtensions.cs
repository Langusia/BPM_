using Core.BPM.Interfaces.Builder;

namespace Core.BPM.Extensions;

public static class OuterNodeBuilderExtensions
{
    public static IOuterNodeBuilderBuilder Or<TCommand>(this IOuterNodeBuilderBuilder builder, Action<IInnerNodeBuilder>? configure = null)
    {
        var p = builder.GetProcess();
        var r = builder.GetRoot();
        var n = new Node(typeof(TCommand), p.ProcessType);
        //add prev to new
        n.AddPrevStep(r);
        //add next to root
        if (r.PrevSteps is not null)
            r.AddNextStep(n);

        configure?.Invoke((builder as IInnerNodeBuilder)!);
        return builder;
    }
}