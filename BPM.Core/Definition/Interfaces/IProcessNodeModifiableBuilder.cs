using System;
using BPM.Core.Process;
using MediatR;

namespace BPM.Core.Definition.Interfaces;

public interface IProcessNodeModifiableBuilder<TProcess> : IProcessNodeInitialBuilder<TProcess> where TProcess : Aggregate
{
    IProcessNodeModifiableBuilder<TProcess> Or<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null) where TCommand : IBaseRequest;
    IProcessNodeModifiableBuilder<TProcess> Or(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure);
    IProcessNodeModifiableBuilder<TProcess> OrAnyTime<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null) where TCommand : IBaseRequest;
    IProcessNodeModifiableBuilder<TProcess> OrJumpTo<TAggregate>(bool sealedSteps = true) where TAggregate : Aggregate;
}