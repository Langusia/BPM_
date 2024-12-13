namespace Core.BPM.DefinitionBuilder;

public interface IProcessNodeInitialBuilder<TProcess> : IProcessNodeBuilder<TProcess> where TProcess : Aggregate
{
    IProcessNodeModifierBuilder<TProcess> Case(Predicate<TProcess> predicate, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>> configure);

    IProcessNodeModifierBuilder<TProcess> Case<TAggregate>(Predicate<TAggregate> predicate, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>> configure)
        where TAggregate : Aggregate;

    IProcessNodeModifierBuilder<TProcess> Continue<Command>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null);
    IProcessNodeModifierBuilder<TProcess> ContinueAnyTime<Command>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null);
    IProcessNodeModifierBuilder<TProcess> ContinueOptional<Command>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null);
}