using System;

namespace Core.BPM.DefinitionBuilder.Interfaces;

public interface IProcessNodeBuilder<T> : IProcessBuilder where T : Aggregate
{
    ProcessConfig<T> End(Action<BProcessConfig>? configureProcess = null);
}