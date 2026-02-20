using System;
using Core.BPM.Application.Managers;
using Core.BPM.DefinitionBuilder;
using Core.BPM.Evaluators.Factory;
using Core.BPM.Persistence;
using Core.BPM.Registry;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Weasel.Core;

namespace Core.BPM.Application;

public static class ServiceCollectionExtensions
{
    public static void AddBpm(this IServiceCollection services, string dbSchemeName, string connectionString, Action<IBpmConfiguration>? configure = null,
        Action<StoreOptions>? configureMartenStore = null)
    {
        var storeOptions = new StoreOptions
        {
            AutoCreateSchemaObjects = AutoCreate.CreateOrUpdate
        };
        storeOptions.Connection(connectionString);
        storeOptions.DatabaseSchemaName = dbSchemeName;
        storeOptions.Events.MetadataConfig.HeadersEnabled = true;
        storeOptions.Events.MetadataConfig.CausationIdEnabled = true;
        storeOptions.Events.MetadataConfig.CorrelationIdEnabled = true;
        configureMartenStore?.Invoke(storeOptions);

        services.AddMarten(storeOptions)
            .UseLightweightSessions();
        services.TryAddScoped(typeof(BpmRepository));
        services.TryAddScoped<IBpmRepository, BpmRepository>();
        services.TryAddScoped<IBpmStore, BpmStore>();
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
        var definition = (TDefinition)FastActivator.CreateAggregate(typeof(TDefinition))!;

        var processDefinition = (ProcessRootBuilder<TAggregate>)ActivatorUtilities.CreateInstance(serviceProvider, typeof(ProcessRootBuilder<>).MakeGenericType(typeof(TAggregate)))!;

        registry.RegisterAggregate(typeof(TAggregate));
        definition.DefineProcess(processDefinition);
    }

    public void AddAggregate<TAggregate>() where TAggregate : Aggregate
    {
        registry.RegisterAggregate(typeof(TAggregate));
    }
}