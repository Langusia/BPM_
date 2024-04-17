using Core.BPM.Configuration;
using Core.BPM.Interfaces.Builder;
using Core.BPM.MediatR.Managers;
using Core.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.BPM.MediatR;

public static class ServiceCollectionExtensions
{
    public static void AddBpm(this IServiceCollection services, Action<IBpmConfiguration>? configure = null)
    {
        services.TryAddScoped(typeof(MartenRepository<>));
        services.TryAddScoped(typeof(BpmGenericProcessManager<>));
        services.TryAddScoped(typeof(BpmManager));
        services.TryAddScoped(typeof(MartenRepository));
        configure?.Invoke(new BpmConfiguration());
    }
}

public interface IBpmDefinition<T> where T : Aggregate
{
    void Define(IProcessBuilder<T> configure);
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
    }
}