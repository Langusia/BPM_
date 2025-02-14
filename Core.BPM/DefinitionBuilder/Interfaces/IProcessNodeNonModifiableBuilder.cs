namespace Core.BPM.DefinitionBuilder;

public interface IProcessNodeNonModifiableBuilder<TProcess> : IProcessNodeInitialBuilder<TProcess> where TProcess : Aggregate
{
}