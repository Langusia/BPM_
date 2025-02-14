namespace Core.BPM.DefinitionBuilder;

public interface IConditionalModifiableBuilder<TProcess> : IProcessNodeModifiableBuilder<TProcess> where TProcess : Aggregate
{
    IProcessNodeModifiableBuilder<TProcess> Else(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure);
}