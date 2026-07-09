using System;
using BPM.Core.Application;
using BPM.Core.Application.Catalog;
using BPM.Core.Application.Execution;
using BPM.Core.Application.Metadata;
using BPM.Core.Application.Persistence;
using BPM.Core.Application.Projection;
using BPM.Core.Process;
using BPM.Core.Definition;
using BPM.Core.Nodes.Evaluation;
using BPM.Core.Persistence;
using JasperFx;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Weasel.Core;

namespace BPM.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBpm(this IServiceCollection services, string dbSchemeName, string connectionString, Action<IBpmConfiguration>? configure = null,
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
        services.TryAddScoped<IProcessStore, ProcessStore>();
        // Phase 1.2: per-request replay context — shared aggregate rehydration +
        // branch-traversal memo consumed by conditional/guest evaluators.
        services.TryAddScoped<IReplayMetrics, ReplayMetrics>();
        services.TryAddScoped<IReplayContext>(sp =>
            new ReplayContext(sp.GetRequiredService<ProcessRegistry>(), sp.GetRequiredService<IReplayMetrics>()));
        services.AddScoped<INodeEvaluatorFactory>(sp =>
            new NodeEvaluatorFactory(sp.GetRequiredService<IBpmRepository>(), sp.GetRequiredService<IReplayContext>()));
        var registry = new ProcessRegistry();
        services.TryAddSingleton(registry);
        services.AddBpmApplicationLayer();
        configure?.Invoke(new BpmConfiguration(registry, services.BuildServiceProvider()));
        return services;
    }

    /// <summary>
    /// Registers the transport-agnostic application layer (command catalog,
    /// metadata resolution, schema projection, execute + next-steps flow) that
    /// agent adapters such as BPM.Mcp build on. Called by AddBpm; safe to call again.
    /// </summary>
    public static IServiceCollection AddBpmApplicationLayer(this IServiceCollection services)
    {
        services.AddOptions();
        services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<BpmAgentOptions>>().Value);
        services.TryAddSingleton<ICommandCatalog>(_ => CommandCatalog.FromRegisteredProcesses());
        services.TryAddSingleton(sp =>
        {
            var options = sp.GetRequiredService<BpmAgentOptions>();
            return new CommandMetadataResolver(options.Specs, options.Identity, options.DefaultPolicy);
        });
        services.TryAddSingleton<CommandSchemaProjector>();
        services.TryAddScoped<IProcessInstanceStore, MartenProcessInstanceStore>();
        // Phase 1.1: per-request capture of dispatched events. Scoped so the
        // write seam (ProcessStore) and the reader (AgentProcessService) share
        // one instance per request — see IExecutionEventCapture docs.
        services.TryAddScoped<IExecutionEventCapture, ExecutionEventCapture>();
        services.TryAddScoped<ICommandDispatcher, MediatRCommandDispatcher>();
        // Phase 1.3: cross-request version-keyed traversal cache (singleton by design).
        services.TryAddSingleton<ITraversalResultCache>(new TraversalResultCache());
        services.TryAddScoped<IAgentProcessService, AgentProcessService>();
        return services;
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
