namespace Core.BPM.DefinitionBuilder.Interfaces;

public interface IProcessNodeNonModifiableBuilder<TProcess> : IProcessNodeInitialBuilder<TProcess> where TProcess : Aggregate
{
}