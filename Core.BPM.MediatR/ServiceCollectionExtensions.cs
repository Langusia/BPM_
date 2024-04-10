using Core.BPM.Configuration;
using Core.BPM.Interfaces.Builder;
using Core.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.BPM.MediatR;

public static class ServiceCollectionExtensions
{
    public static void AddBpm(this IServiceCollection services, Action<IBpmConfiguration> configure = null)
    {
        var config = new BpmConfiguration();
        services.TryAddScoped(typeof(MartenRepository<>));
        services.TryAddScoped(typeof(BpmProcessManager<>));
        configure.Invoke(new BpmConfiguration());
    }
}

public interface IBpmDefinition<T> where T : Aggregate
{
    void Define(IProcessBuilder<T> configure);
}

public interface IBpmDefinition
{
    void Define(IProcessBuilder configure);
}

public interface IBpmConfiguration
{
    void AddAggregateDefinition<TAggregate, TDefinition>() where TAggregate : Aggregate
        where TDefinition : IBpmDefinition<TAggregate>;
}

public class BpmConfiguration : IBpmConfiguration
{
    public void AddAggregateDefinition<TAggregate, TDefinition>() where TAggregate : Aggregate
        where TDefinition : IBpmDefinition<TAggregate>
    {
        var definition = (TDefinition)Activator.CreateInstance(typeof(TDefinition))!;
        var processDefinition = (ProcessBuilder<TAggregate>)Activator.CreateInstance(typeof(ProcessBuilder<>).MakeGenericType(typeof(TAggregate)))!;

        definition.Define(processDefinition);

        //var configuratorInst = (BpmProcessGraphConfigurator)Activator.CreateInstance(
        //aggregateDefinitions.Add(typeof(TAggregate), typeof(TDefinition));
    }
}