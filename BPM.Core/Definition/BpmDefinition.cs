using BPM.Core.Definition.Interfaces;
using BPM.Core.Process;

namespace BPM.Core.Definition;

public interface IBpmDefinition;

public abstract class BpmDefinition<T> : IBpmDefinition where T : Aggregate
{
    public abstract ProcessConfig<T> DefineProcess(IProcessBuilder<T> configureProcess);
}
