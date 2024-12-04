namespace Core.BPM.DefinitionBuilder;

public interface IProcessNodeModifierBuilder<TProcess> : IProcessNodeInitialBuilder<TProcess> where TProcess : Aggregate
{
    IProcessNodeModifierBuilder<TProcess> Or<TCommand>(Action<IProcessScopedNodeInitialBuilder<TProcess>>? configure = null);
    IProcessNodeModifierBuilder<TProcess> OrOptional<TCommand>(Action<IProcessScopedNodeInitialBuilder<TProcess>>? configure = null);
    IProcessNodeModifierBuilder<TProcess> OrAnyTime<TCommand>(Action<IProcessScopedNodeInitialBuilder<TProcess>>? configure = null);
}