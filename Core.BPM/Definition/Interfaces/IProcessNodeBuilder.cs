using System;
using Core.BPM.Process;

namespace Core.BPM.Definition.Interfaces;

public interface IProcessNodeBuilder<T> : IProcessBuilder where T : Aggregate
{
    ProcessConfig<T> End(Action<BProcessConfig>? configureProcess = null);
}