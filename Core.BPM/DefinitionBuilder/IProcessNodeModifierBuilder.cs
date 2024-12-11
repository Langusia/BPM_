namespace Core.BPM.DefinitionBuilder;

public interface IProcessNodeModifierBuilder<TProcess> : IProcessNodeInitialBuilder<TProcess>, INon where TProcess : Aggregate
{
    IProcessNodeModifierBuilder<TProcess> Or<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null);
    IProcessNodeModifierBuilder<TProcess> OrOptional<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null);
    IProcessNodeModifierBuilder<TProcess> OrAnyTime<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null);
}

public interface INon
{
}

public static class Ext
{
    public static IProcessNodeModifierBuilder<TProcess> Test<TProcess, TCommand>(this IProcessNodeModifierBuilder<TProcess> builder) where TProcess : Aggregate
    {
        return null;
    }
}