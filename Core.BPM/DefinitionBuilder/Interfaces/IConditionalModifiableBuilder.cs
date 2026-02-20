using System;

namespace Core.BPM.DefinitionBuilder.Interfaces;

public interface IConditionalModifiableBuilder<TProcess> : IProcessNodeModifiableBuilder<TProcess> where TProcess : Aggregate
{
    IProcessNodeModifiableBuilder<TProcess> Else(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure);

    /// <summary>
    /// Defines the else branch allowing non-modifiable builders (e.g. UnlockOptional) as the branch result.
    /// </summary>
    IProcessNodeModifiableBuilder<TProcess> Else(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeNonModifiableBuilder<TProcess>> configure);
}