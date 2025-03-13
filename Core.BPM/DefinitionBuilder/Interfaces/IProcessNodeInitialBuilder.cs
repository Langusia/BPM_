using MediatR;

namespace Core.BPM.DefinitionBuilder.Interfaces;

public interface IProcessNodeInitialBuilder<TProcess> : IProcessNodeBuilder<TProcess> where TProcess : Aggregate
{
    IProcessNodeModifiableBuilder<TProcess> Group(Action<IGroupBuilder<TProcess>> configure);
    IProcessNodeModifiableBuilder<TProcess> JumpTo<TGuestAggregate>() where TGuestAggregate : Aggregate;
    IConditionalModifiableBuilder<TProcess> If(Predicate<TProcess> predicate, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure);
    IConditionalModifiableBuilder<TProcess> If<T>(Predicate<T> predicate, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure) where T : Aggregate;

    IProcessNodeModifiableBuilder<TProcess> Continue<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null) where TCommand : IBaseRequest;

    IProcessNodeModifiableBuilder<TProcess> ContinueAnyTime<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null)
        where TCommand : IBaseRequest;

    //noneModifiable
    IProcessNodeNonModifiableBuilder<TProcess> UnlockOptional<TCommand>() where TCommand : IBaseRequest;
}