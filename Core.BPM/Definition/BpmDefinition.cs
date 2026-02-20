using Core.BPM.Definition.Interfaces;
using Core.BPM.Process;

namespace Core.BPM.Definition;

public interface IBpmDefinition;

public abstract class BpmDefinition<T> : IBpmDefinition where T : Aggregate
{
    public abstract ProcessConfig<T> DefineProcess(IProcessBuilder<T> configureProcess);
}
