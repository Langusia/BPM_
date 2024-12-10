namespace Core.BPM.DefinitionBuilder;

public interface IProcessNodeModifierBuilder : IProcessNodeInitialBuilder where TProcess : Aggregate
{
    IProcessNodeModifierBuilder<TProcess> Or<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null);
    IProcessNodeModifierBuilder<TProcess> OrOptional<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null);
    IProcessNodeModifierBuilder<TProcess> OrAnyTime<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null);
}