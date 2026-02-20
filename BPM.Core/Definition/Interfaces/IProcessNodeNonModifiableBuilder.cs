using BPM.Core.Process;

namespace BPM.Core.Definition.Interfaces;

public interface IProcessNodeNonModifiableBuilder<TProcess> : IProcessNodeInitialBuilder<TProcess> where TProcess : Aggregate
{
}