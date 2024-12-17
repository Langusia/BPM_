namespace Core.BPM.DefinitionBuilder;

public interface IProcessNodeModifiableBuilder<TProcess> : IProcessNodeInitialBuilder<TProcess> where TProcess : Aggregate
{
    IProcessNodeModifiableBuilder<TProcess> Or<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null);

    [Obsolete("OrOptional is deprecated, please use Or instead.")]
    IProcessNodeModifiableBuilder<TProcess> OrOptional<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null);

    IProcessNodeModifiableBuilder<TProcess> OrAnyTime<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null);
}