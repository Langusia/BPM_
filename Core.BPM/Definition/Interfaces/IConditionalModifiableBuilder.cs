using System;
using Core.BPM.Process;

namespace Core.BPM.Definition.Interfaces;

public interface IConditionalModifiableBuilder<TProcess> : IProcessNodeModifiableBuilder<TProcess> where TProcess : Aggregate
{
    IProcessNodeModifiableBuilder<TProcess> Else(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure);

    /// <summary>
    /// Defines the else branch allowing non-modifiable builders (e.g. UnlockOptional) as the branch result.
    /// </summary>
    IProcessNodeModifiableBuilder<TProcess> Else(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeNonModifiableBuilder<TProcess>> configure);
}