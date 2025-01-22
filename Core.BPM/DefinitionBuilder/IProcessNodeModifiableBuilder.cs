namespace Core.BPM.DefinitionBuilder;

public interface IProcessNodeModifiableBuilder<TProcess> : IProcessNodeInitialBuilder<TProcess> where TProcess : Aggregate
{
    /// <summary>
    /// Defines a parallel scope where step execution order is not enforced but all steps must complete.
    /// </summary>
    IProcessNodeModifiableBuilder<TProcess> ParallelScope(Action<IParallelScopeBuilder<TProcess>> configure);

    IProcessNodeModifiableBuilder<TProcess> Or<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null);
    IProcessNodeModifiableBuilder<TProcess> Or(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure);

    [Obsolete("OrOptional is deprecated, please use Or instead.")]
    IProcessNodeModifiableBuilder<TProcess> OrOptional<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null);

    IProcessNodeModifiableBuilder<TProcess> OrAnyTime<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null);
}