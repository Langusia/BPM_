namespace Core.BPM.DefinitionBuilder;

public interface IProcessNodeInitialBuilder<TProcess> : IProcessNodeBuilder<TProcess> where TProcess : Aggregate
{
    IProcessNodeModifiableBuilder<TProcess> Case(Predicate<TProcess> predicate, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure);

    IProcessNodeModifiableBuilder<TProcess> Case<TAggregate>(Predicate<TAggregate> predicate, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure)
        where TAggregate : Aggregate;

    IProcessNodeModifiableBuilder<TProcess> Continue<Command>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null);

    IProcessNodeModifiableBuilder<TProcess> ContinueAnyTime<Command>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null);

    //noneModifiable
    IProcessNodeNonModifiableBuilder<TProcess> UnlockOptional<Command>();
}