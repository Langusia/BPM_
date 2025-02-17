using Core.BPM.DefinitionBuilder;
using Core.BPM.DefinitionBuilder.Interfaces;
using Core.BPM.Trash;
using MediatR;

namespace Core.BPM.Application;

public interface IBpmDefinition;

public abstract class BpmDefinition<T> : IBpmDefinition where T : Aggregate
{
    public abstract ProcessConfig<T> DefineProcess(IProcessBuilder<T> configureProcess);

    public virtual void ConfigureSteps(StepConfigurator<T> stepConfigurator)
    {
    }
}