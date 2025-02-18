﻿namespace Core.BPM.DefinitionBuilder.Interfaces;

public interface IProcessNodeInitialBuilder<TProcess> : IProcessNodeBuilder<TProcess> where TProcess : Aggregate
{
    IProcessNodeModifiableBuilder<TProcess> Group(Action<IGroupBuilder<TProcess>> configure);

    IConditionalModifiableBuilder<TProcess> If(Predicate<TProcess> predicate, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure);
    IConditionalModifiableBuilder<TProcess> If<T>(Predicate<T> predicate, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure) where T : Aggregate;

    IProcessNodeModifiableBuilder<TProcess> Continue<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null);

    IProcessNodeModifiableBuilder<TProcess> ContinueAnyTime<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null);

    //noneModifiable
    IProcessNodeNonModifiableBuilder<TProcess> UnlockOptional<TCommand>();
}