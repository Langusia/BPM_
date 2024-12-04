namespace Core.BPM.DefinitionBuilder;

public interface IProcessScopedNodeInitialBuilder<TProcess> where TProcess : Aggregate
{
    IProcessNodeModifierBuilder<TProcess> ThenContinue<TCommand>(Action<IProcessScopedNodeInitialBuilder<TProcess>>? configure = null);
    IProcessNodeModifierBuilder<TProcess> ThenContinueAnyTime<TCommand>(Action<IProcessScopedNodeInitialBuilder<TProcess>>? configure = null);
    IProcessNodeModifierBuilder<TProcess> ThenContinueOptional<TCommand>(Action<IProcessScopedNodeInitialBuilder<TProcess>>? configure = null);
}