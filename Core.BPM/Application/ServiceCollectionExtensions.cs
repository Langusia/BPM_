using Core.BPM.Application.Managers;
using Core.BPM.BCommand;
using Core.BPM.MediatR.Managers;
using Core.BPM.Persistence;
using Core.Persistence;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.BPM.MediatR;

public static class ServiceCollectionExtensions
{
    public static void AddBpm(this IServiceCollection services, Action<StoreOptions> configureMartenStore, Action<IBpmConfiguration>? configure = null)
    {
        services.AddMarten(configureMartenStore);
        services.TryAddScoped(typeof(MartenRepository<>));
        services.TryAddScoped(typeof(BpmGenericProcessManager<>));
        services.TryAddScoped(typeof(BpmStore<,>));
        services.TryAddScoped(typeof(MartenRepository));
        services.TryAddScoped(typeof(BpmEventConfigurationBuilder<>));
        services.AddOptions<BpmEventConfiguration>();
        configure?.Invoke(new BpmConfiguration());
    }
}

public interface IBpmConfiguration
{
    void AddAggregateDefinition<TAggregate, TDefinition>() where TAggregate : Aggregate
        where TDefinition : BpmDefinition<TAggregate>;
}

public class BpmConfiguration : IBpmConfiguration
{
    public void AddAggregateDefinition<TAggregate, TDefinition>() where TAggregate : Aggregate
        where TDefinition : BpmDefinition<TAggregate>
    {
        var definition = (TDefinition)Activator.CreateInstance(typeof(TDefinition))!;
        var processDefinition = (ProcessBuilder<TAggregate>)Activator.CreateInstance(typeof(ProcessBuilder<>).MakeGenericType(typeof(TAggregate)))!;
        var eventBuilder = (BpmEventConfigurationBuilder<TAggregate>)Activator.CreateInstance(typeof(BpmEventConfigurationBuilder<>).MakeGenericType(typeof(TAggregate)))!;
        definition.DefineProcess(processDefinition);
        definition.SetEventConfiguration(eventBuilder);
    }
}