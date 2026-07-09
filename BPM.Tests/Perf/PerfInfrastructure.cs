using System.Reflection;
using System.Threading;
using BPM.Contracts;
using BPM.Core.Attributes;
using BPM.Core.Application;
using BPM.Core.Application.Catalog;
using BPM.Core.Application.Execution;
using BPM.Core.Application.Metadata;
using BPM.Core.Application.Persistence;
using BPM.Core.Application.Projection;
using BPM.Core.Configuration;
using BPM.Core.Definition;
using BPM.Core.Events;
using BPM.Core.Nodes;
using BPM.Core.Nodes.Evaluation;
using BPM.Core.Persistence;
using BPM.Core.Process;
using BPM.Tests.Application;
using JasperFx.Events;
using NSubstitute;

namespace BPM.Tests.Perf;

/// <summary>
/// The 1.0 measurement seams. Counts what is observable WITHOUT touching
/// BPM.Core: stream loads (store seam) and evaluator-driven aggregate
/// replays (IBpmRepository seam — conditional + guest evaluators route
/// through it). Traversal counts arrive with ReplayContext in Phase 1.2,
/// when the in-core IReplayMetrics lands.
/// </summary>
public sealed class PerfCounters
{
    private int _streamLoads;
    private int _evaluatorReplays;

    public int StreamLoads => _streamLoads;
    public int EvaluatorReplays => _evaluatorReplays;

    public void CountLoad() => Interlocked.Increment(ref _streamLoads);
    public void CountReplay() => Interlocked.Increment(ref _evaluatorReplays);

    public void Reset()
    {
        _streamLoads = 0;
        _evaluatorReplays = 0;
    }
}

/// <summary>Counting decorator over the application-layer store seam.</summary>
public sealed class CountingInstanceStore(IProcessInstanceStore inner, PerfCounters counters) : IProcessInstanceStore
{
    public Task<ProcessInstanceSnapshot?> LoadAsync(Guid processId, CancellationToken ct)
    {
        counters.CountLoad();
        return inner.LoadAsync(processId, ct);
    }
}

/// <summary>
/// Real replaying IBpmRepository (registry-backed, same as the in-memory one
/// used by the MCP integration tests) with replay counting. Conditional and
/// guest evaluators call this to rehydrate aggregates — every call counted
/// here is a full stream replay we expect Phase 1.2 to eliminate.
/// </summary>
public sealed class CountingBpmRepository(ProcessRegistry registry, PerfCounters counters) : IBpmRepository
{
    // Event-store side: not used by the perf path.
    public Task<IReadOnlyList<IEvent>> FetchStreamAsync(Guid id, CancellationToken ct) =>
        throw new NotSupportedException("Not used by the perf fixture.");

    public Task<T?> AggregateStreamAsync<T>(Guid id, CancellationToken ct) where T : Aggregate =>
        throw new NotSupportedException("Not used by the perf fixture.");

    public Task AppendEvents(Guid aggregateId, object[] events, bool newStream = true,
        Dictionary<string, object>? headers = null, CancellationToken ct = default) =>
        Task.CompletedTask;

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;

    // Registry side: the replay hot path.
    public object AggregateStreamFromRegistry(Type aggregateType, IEnumerable<object> events)
    {
        counters.CountReplay();
        var aggregate = FastActivator.CreateAggregate(aggregateType)!;
        var applied = 0;
        foreach (var @event in events)
        {
            var apply = registry.GetApplyMethodOrNull(aggregateType, @event.GetType());
            if (apply is null)
                continue;
            applied++;
            apply(aggregate, @event);
        }

        if (applied == 0)
            throw new InvalidOperationException($"No Apply methods found for aggregate {aggregateType.Name}.");
        return aggregate;
    }

    public object? AggregateOrNullStreamFromRegistry(Type aggregateType, IEnumerable<object> events)
    {
        counters.CountReplay();
        var aggregate = FastActivator.CreateAggregate(aggregateType)!;
        foreach (var @event in events)
        {
            var apply = registry.GetApplyMethodOrNull(aggregateType, @event.GetType());
            if (apply is null)
                return null;
            apply(aggregate, @event);
        }

        return aggregate;
    }

    public object AggregateOrDefaultStreamFromRegistry(Type aggregateType, IEnumerable<object> events)
    {
        counters.CountReplay();
        var aggregate = FastActivator.CreateAggregate(aggregateType)!;
        foreach (var @event in events)
            registry.GetApplyMethodOrNull(aggregateType, @event.GetType())?.Invoke(aggregate, @event);
        return aggregate;
    }

    public bool TryAggregateAs<T>(out T? aggregate, IEnumerable<object> events) where T : Aggregate
    {
        aggregate = null;
        try
        {
            aggregate = (T)AggregateStreamFromRegistry(typeof(T), events);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool TryAggregateAs(Type aggType, IEnumerable<object> stream, out Aggregate? aggregate)
    {
        aggregate = null;
        try
        {
            aggregate = (Aggregate)AggregateStreamFromRegistry(aggType, stream);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

/// <summary>
/// Like GraphTestBase, but wired with a REAL replaying repository (the perf
/// fixture's conditionals and guest process need actual aggregates, which the
/// NSubstitute repo can't provide) plus the counting seams.
/// </summary>
public abstract class PerfGraphBase : IDisposable
{
    protected readonly PerfCounters Counters = new();
    protected readonly ProcessRegistry Registry = new();
    protected readonly InMemoryInstanceStore InnerStore = new();
    protected readonly CountingInstanceStore Store;
    protected readonly CountingBpmRepository Repository;
    protected readonly INodeEvaluatorFactory EvaluatorFactory;
    protected readonly FakeDispatcher Dispatcher = new();
    protected readonly BpmAgentOptions Options = new();
    protected readonly ExecutionEventCapture Capture = new();
    protected readonly ReplayMetrics Metrics = new();
    protected readonly TraversalResultCache TraversalCache = new();

    protected PerfGraphBase()
    {
        ClearProcesses();
        Repository = new CountingBpmRepository(Registry, Counters);
        Store = new CountingInstanceStore(InnerStore, Counters);
        // Phase 1.2: evaluators consume the request-scoped ReplayContext; the
        // counting repository seam should now stay silent during traversal.
        EvaluatorFactory = new NodeEvaluatorFactory(Repository, new ReplayContext(Registry, Metrics));

        BuildDefinition<ComplianceReview, ComplianceReviewDefinition>();
        BuildDefinition<QuickAudit, QuickAuditDefinition>();
        BuildDefinition<KitchenSink, KitchenSinkDefinition>();

        // Simulate the sanctioned write path (Phase 1.1): a dispatched command
        // "appends" its [BpmProducer]-declared events, NodeId-stamped exactly as
        // Process.AppendEvents would, and ProcessStore records them into the
        // scoped capture. The in-memory store itself is deliberately NOT
        // appended to: benches execute the same command repeatedly against a
        // stable stream.
        Dispatcher.OnDispatch = command =>
        {
            var processId = command.GetType().GetProperty("ProcessId")?.GetValue(command) as Guid?;
            if (processId is null || processId == Guid.Empty)
                return Task.FromResult<object?>(null);

            var produced = new List<object>();
            foreach (var attr in command.GetType().GetCustomAttributes(typeof(BpmProducer), false).Cast<BpmProducer>())
            foreach (var eventType in attr.EventTypes)
            {
                if (eventType.GetConstructor(Type.EmptyTypes) is null)
                    continue; // parameterized events aren't auto-instantiable; tests dispatching them record manually
                var e = (BpmEvent)Activator.CreateInstance(eventType)!;
                var level = TryLevelOf(command.GetType());
                if (level is not null)
                    e.NodeId = level.Value;
                produced.Add(e);
            }

            if (produced.Count > 0)
                Capture.Record(processId.Value, produced);
            return Task.FromResult<object?>(null);
        };
    }

    /// <summary>NodeLevel of the node producing for a command, across all fixture processes.</summary>
    private static int? TryLevelOf(Type commandType)
    {
        foreach (var name in new[] { nameof(KitchenSink), nameof(ComplianceReview), nameof(QuickAudit) })
        {
            var config = BProcessGraphConfiguration.GetConfig(name);
            if (config is null)
                continue;
            var node = KitchenSinkStreams.DeepNodes(config.RootNode, new HashSet<INode>())
                .FirstOrDefault(n => n.CommandType == commandType);
            if (node is not null)
                return node.NodeLevel;
        }

        return null;
    }

    public void Dispose() => ClearProcesses();

    protected void BuildDefinition<T, TDefinition>()
        where T : Aggregate
        where TDefinition : BpmDefinition<T>, new()
    {
        new TDefinition().DefineProcess(new ProcessRootBuilder<T>(EvaluatorFactory));
        Registry.RegisterAggregate(typeof(T));
    }

    /// <summary>
    /// Rebuilds the fixture graphs with a different evaluator factory (nodes
    /// capture their factory at build time). Used by parity tests to compare
    /// context-wired vs legacy evaluator behavior on identical graph shapes.
    /// Aggregates stay registered; only the static graph config is rebuilt.
    /// </summary>
    protected void RebuildDefinitions(INodeEvaluatorFactory factory)
    {
        ClearProcesses();
        new ComplianceReviewDefinition().DefineProcess(new ProcessRootBuilder<ComplianceReview>(factory));
        new QuickAuditDefinition().DefineProcess(new ProcessRootBuilder<QuickAudit>(factory));
        new KitchenSinkDefinition().DefineProcess(new ProcessRootBuilder<KitchenSink>(factory));
    }

    protected AgentProcessService CreateService()
    {
        var catalog = CommandCatalog.FromRegisteredProcesses();
        var resolver = new CommandMetadataResolver(Options.Specs, Options.Identity, Options.DefaultPolicy);
        return new AgentProcessService(
            catalog,
            Store,
            Dispatcher,
            resolver,
            new CommandSchemaProjector(resolver),
            Options,
            Registry,
            Substitute.For<IServiceProvider>(),
            new CapturingLogger<AgentProcessService>(),
            Capture,
            TraversalCache);
    }

    private static void ClearProcesses()
    {
        var field = typeof(BProcessGraphConfiguration)
            .GetField("_processes", BindingFlags.Static | BindingFlags.NonPublic)!;
        field.SetValue(null, null);
    }
}
