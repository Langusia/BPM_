namespace Core.BPM.DefinitionBuilder;

public interface IProcessNodeInitialBuilder<TProcess> : IProcessNodeBuilder<TProcess> where TProcess : Aggregate
{
    IProcessNodeModifiableBuilder<TProcess> Group(Action<IGroupBuilder<TProcess>> configure);
    IProcessNodeModifiableBuilder<TProcess> Case(Predicate<TProcess> predicate, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure);

    IConditionalModifiableBuilder<TProcess> If(Predicate<TProcess> predicate, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure);

    IProcessNodeModifiableBuilder<TProcess> Case<TAggregate>(Predicate<TAggregate> predicate, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure)
        where TAggregate : Aggregate;

    IProcessNodeModifiableBuilder<TProcess> Continue<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null);

    IProcessNodeModifiableBuilder<TProcess> ContinueAnyTime<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null);

    //noneModifiable
    IProcessNodeNonModifiableBuilder<TProcess> UnlockOptional<TCommand>();
}