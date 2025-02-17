using Core.BPM.Application.Managers;
using Core.BPM.DefinitionBuilder;
using Core.BPM.Evaluators.Factory;
using Core.BPM.Persistence;
using Core.BPM.Registry;
using Core.BPM.Trash;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StepConfigurator = Core.BPM.Trash.StepConfigurator;

namespace Core.BPM.Application;

public static class ServiceCollectionExtensions
{
    public static void AddBpm(this IServiceCollection services, Action<StoreOptions> configureMartenStore, Action<IBpmConfiguration>? configure = null)
    {
        services.AddMarten(configureMartenStore);
        services.TryAddScoped(typeof(BpmRepository));
        services.TryAddScoped(typeof(BpmEventConfigurationBuilder<>));
        services.TryAddScoped<IBpmRepository, BpmRepository>();
        services.TryAddScoped<IBpmStore, BpmStore>();
        services.AddOptions<StepConfigurator>();
        services.AddScoped<INodeEvaluatorFactory, NodeEvaluatorFactory>();
        var registry = new ProcessRegistry();
        services.TryAddSingleton(registry);
        configure?.Invoke(new BpmConfiguration(registry, services.BuildServiceProvider()));
    }
}

public interface IBpmConfiguration
{
    void AddAggregateDefinition<TAggregate, TDefinition>() where TAggregate : Aggregate
        where TDefinition : BpmDefinition<TAggregate>;

    void AddAggregate<TAggregate>() where TAggregate : Aggregate;
}

public class BpmConfiguration(ProcessRegistry registry, IServiceProvider serviceProvider) : IBpmConfiguration
{
    public void AddAggregateDefinition<TAggregate, TDefinition>() where TAggregate : Aggregate
        where TDefinition : BpmDefinition<TAggregate>
    {
        var definition = (TDefinition)Activator.CreateInstance(typeof(TDefinition))!;

        var processDefinition = (ProcessBuilder<TAggregate>)ActivatorUtilities.CreateInstance(serviceProvider, typeof(ProcessBuilder<>).MakeGenericType(typeof(TAggregate)))!;
        var stepConfigurator = (StepConfigurator<TAggregate>)Activator.CreateInstance(typeof(StepConfigurator<>).MakeGenericType(typeof(TAggregate)))!;

        registry.RegisterAggregate(typeof(TAggregate));
        definition.ConfigureSteps(stepConfigurator);
        definition.DefineProcess(processDefinition);
    }

    public void AddAggregate<TAggregate>() where TAggregate : Aggregate
    {
        registry.RegisterAggregate(typeof(TAggregate));
    }
}