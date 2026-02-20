using System;
using BPM.Core.Process;

namespace BPM.Core.Definition.Interfaces;

public interface IGroupBuilder<TProcess> where TProcess : Aggregate
{
    void AddStep<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null);
    void AddAnyTime<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null);
}