using Core.BPM.DefinitionBuilder;
using Core.BPM.DefinitionBuilder.Interfaces;
using MediatR;

namespace Core.BPM.Application;

public interface IBpmDefinition;

public abstract class BpmDefinition<T> : IBpmDefinition where T : Aggregate
{
    public abstract ProcessConfig<T> DefineProcess(IProcessBuilder<T> configureProcess);
}