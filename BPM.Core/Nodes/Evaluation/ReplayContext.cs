using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using BPM.Core.Persistence;
using BPM.Core.Process;

namespace BPM.Core.Nodes.Evaluation;

/// <summary>
/// OPTIMIZE Phase 1.2 — per-request replay context.
///
/// During graph traversal, conditional and guest-process evaluators need
/// rehydrated aggregates and branch-completion results. Historically each node
/// visit replayed the full event stream through <see cref="IBpmRepository"/>
/// (measured: 16 replays per execute on the kitchen-sink fixture). This context
/// rehydrates each aggregate type AT MOST ONCE per stream state per request and
/// memoizes branch traversals, and the evaluators consume it instead of the
/// repository.
///
/// Stream identity is the <see cref="List{T}"/> INSTANCE (reference), not a
/// hash: the same list flowing through one traversal is one stream state; a
/// different/extended list (e.g. the post-dispatch synthesized snapshot) is a
/// different state and gets a fresh scope. This deliberately avoids the flaw
/// that killed the old NodeBase cache (hashing event types, blind to data).
///
/// Register SCOPED — one instance per request. Not thread-safe by design.
/// </summary>
public interface IReplayContext
{
    /// <summary>Aggregate of <paramref name="aggregateType"/> for this exact stream, rehydrated at most once.</summary>
    object GetAggregate(Type aggregateType, List<object> storedEvents);

    /// <summary>
    /// TryAggregateAs semantics (guest processes): false when no event in the
    /// stream applies to the type. The failure is also cached — retried at most never.
    /// </summary>
    bool TryGetAggregate(Type aggregateType, List<object> storedEvents, out Aggregate? aggregate);

    /// <summary>Branch-traversal memo: compute once per (node, stream instance).</summary>
    (bool isComplete, List<INode> availableNodes) GetOrAddTraversal(
        INode node, List<object> storedEvents, Func<(bool isComplete, List<INode> availableNodes)> compute);
}

/// <summary>
/// In-core replay accounting, introduced with the ReplayContext (Phase 1.2).
/// <see cref="AggregateRehydrations"/> counts context cache misses — the real
/// replay work still performed. The Phase 1.3 cross-request cache drives the
/// per-read rehydration count to zero for repeated same-version reads.
/// </summary>
public interface IReplayMetrics
{
    int AggregateRehydrations { get; }
    int TraversalMemoHits { get; }
    void CountRehydration();
    void CountTraversalHit();
    void Reset();
}

public sealed class ReplayMetrics : IReplayMetrics
{
    public int AggregateRehydrations { get; private set; }
    public int TraversalMemoHits { get; private set; }

    public void CountRehydration() => AggregateRehydrations++;
    public void CountTraversalHit() => TraversalMemoHits++;

    public void Reset()
    {
        AggregateRehydrations = 0;
        TraversalMemoHits = 0;
    }
}

public sealed class ReplayContext(ProcessRegistry registry, IReplayMetrics? metrics = null) : IReplayContext
{
    // ConditionalWeakTable: scopes are GC-collected together with their stream
    // lists — no unbounded growth across a long-lived (mis-registered) instance.
    private readonly ConditionalWeakTable<List<object>, StreamScope> _scopes = new();

    private sealed class StreamScope
    {
        // Value is the rehydrated aggregate, or null when TryGetAggregate failed
        // (no applicable events) — the negative result is cached too.
        public readonly Dictionary<Type, object?> Aggregates = new();
        public readonly Dictionary<INode, (bool isComplete, List<INode> availableNodes)> Traversals = new();
    }

    private StreamScope ScopeFor(List<object> storedEvents) => _scopes.GetOrCreateValue(storedEvents);

    public object GetAggregate(Type aggregateType, List<object> storedEvents)
    {
        var scope = ScopeFor(storedEvents);
        if (scope.Aggregates.TryGetValue(aggregateType, out var cached) && cached is not null)
            return cached;

        // Mirrors IBpmRepository.AggregateOrDefaultStreamFromRegistry: apply
        // whatever matches; zero matches still yields a default aggregate.
        var aggregate = Rehydrate(aggregateType, storedEvents, out _);
        scope.Aggregates[aggregateType] = aggregate;
        return aggregate;
    }

    public bool TryGetAggregate(Type aggregateType, List<object> storedEvents, out Aggregate? aggregate)
    {
        var scope = ScopeFor(storedEvents);
        if (scope.Aggregates.TryGetValue(aggregateType, out var cached))
        {
            aggregate = cached as Aggregate;
            return cached is not null;
        }

        // Mirrors IBpmRepository.TryAggregateAs: success requires >= 1 applied event.
        var candidate = Rehydrate(aggregateType, storedEvents, out var applied);
        if (applied == 0)
        {
            scope.Aggregates[aggregateType] = null; // cache the failure
            aggregate = null;
            return false;
        }

        scope.Aggregates[aggregateType] = candidate;
        aggregate = candidate as Aggregate;
        return aggregate is not null;
    }

    public (bool isComplete, List<INode> availableNodes) GetOrAddTraversal(
        INode node, List<object> storedEvents, Func<(bool isComplete, List<INode> availableNodes)> compute)
    {
        var scope = ScopeFor(storedEvents);
        if (scope.Traversals.TryGetValue(node, out var cached))
        {
            metrics?.CountTraversalHit();
            return cached;
        }

        var result = compute();
        scope.Traversals[node] = result;
        return result;
    }

    private object Rehydrate(Type aggregateType, List<object> storedEvents, out int applied)
    {
        metrics?.CountRehydration();
        var aggregate = FastActivator.CreateAggregate(aggregateType)!;
        applied = 0;
        foreach (var @event in storedEvents)
        {
            var apply = registry.GetApplyMethodOrNull(aggregateType, @event.GetType());
            if (apply is null)
                continue;
            applied++;
            apply(aggregate, @event);
        }

        return aggregate;
    }
}
