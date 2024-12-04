namespace Core.BPM.DefinitionBuilder;

public interface IProcessNodeInitialBuilder<TProcess> : IProcessNodeBuilder<TProcess> where TProcess : Aggregate
{
    IProcessNodeModifierBuilder<TProcess> Case(Predicate<TProcess> predicate, Action<IProcessNodeInitialBuilder<TProcess>> configure);
    IProcessNodeModifierBuilder<TProcess> Continue<Command>(Action<IProcessScopedNodeInitialBuilder<TProcess>>? configure = null);
    IProcessNodeModifierBuilder<TProcess> ContinueAnyTime<Command>(Action<IProcessScopedNodeInitialBuilder<TProcess>>? configure = null);
    IProcessNodeModifierBuilder<TProcess> ContinueOptional<Command>(Action<IProcessScopedNodeInitialBuilder<TProcess>>? configure = null);
}