using Core.BPM.BCommand;
using Core.BPM.DefinitionBuilder;

namespace Core.BPM.Application;

public interface IBpmDefinition<T> where T : Aggregate
{
    void DefineProcess(IProcessBuilder<T> configureProcess);
}

public abstract class BpmDefinition<T> : IBpmDefinition<T> where T : Aggregate
{
    public abstract void DefineProcess(IProcessBuilder<T> configureProcess);

    public virtual void SetEventConfiguration(BpmEventConfigurationBuilder<T> bpmEventConfiguration)
    {
    }
}