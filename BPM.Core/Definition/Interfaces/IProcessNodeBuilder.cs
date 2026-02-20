using System;
using BPM.Core.Process;

namespace BPM.Core.Definition.Interfaces;

public interface IProcessNodeBuilder<T> : IProcessBuilder where T : Aggregate
{
    ProcessConfig<T> End(Action<BProcessConfig>? configureProcess = null);
}