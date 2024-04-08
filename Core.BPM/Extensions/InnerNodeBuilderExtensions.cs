using Core.BPM.Interfaces.Builder;

namespace Core.BPM.Extensions;

public static class InnerNodeBuilderExtensions
{
    public static IInnerNodeBuilder Or<TCommand>(this IInnerNodeBuilder builder, Action<INodeBuilder>? configure = null)
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
}