using Core.BPM.Process;

namespace Core.BPM.Definition.Interfaces;

public interface IProcessNodeNonModifiableBuilder<TProcess> : IProcessNodeInitialBuilder<TProcess> where TProcess : Aggregate
{
}