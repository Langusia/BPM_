using Core.BPM.BCommand;
using Core.BPM.DefinitionBuilder;
using MediatR;

namespace Core.BPM.Application;

public interface IBpmDefinition<T> where T : Aggregate
{
    MyClass<T> DefineProcess(IProcessBuilder<T> configureProcess);
}

public abstract class BpmDefinition<T> : IBpmDefinition<T> where T : Aggregate
{
    public abstract MyClass<T> DefineProcess(IProcessBuilder<T> configureProcess);

    public virtual void ConfigureSteps(StepConfigurator<T> stepConfigurator)
    {
    }
}