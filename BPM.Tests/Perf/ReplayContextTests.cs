using BPM.Core.Application.Execution;
using BPM.Core.Nodes.Evaluation;
using BPM.Tests.Application;
using Xunit;

namespace BPM.Tests.Perf;

/// <summary>
/// Phase 1.2 behavior tests — GREEN from the moment ReplayContext lands.
/// The most important one is the PARITY test: the engine's answers (NextSteps,
/// state) must be identical with and without the context. The context is an
/// optimization; any output difference is a correctness bug, not a trade-off.
/// </summary>
public class ReplayContextTests : PerfGraphBase
{
    private const int StreamLength = 200;

    // ---- correctness pin: identical behavior with and without the context ----

    [Fact]
    public async Task Parity_next_steps_identical_with_and_without_context()
    {
        // WITH context (default PerfGraphBase wiring):
        var processId = Guid.NewGuid();
        InnerStore.Seed(processId, nameof(KitchenSink),
            KitchenSinkStreams.ReadyToClose(StreamLength).Cast<object>().ToArray());
        var withContext = await CreateService().GetProcessAsync(processId, CancellationToken.None);
        Assert.True(withContext.Ok, withContext.Error?.Message);

        // WITHOUT context (legacy repository path) — same graph definitions,
        // rebuilt fresh so nodes carry legacy evaluator factories:
        using var legacy = new LegacyGraphFixture();
        var legacyProcessId = Guid.NewGuid();
        legacy.InnerStore.Seed(legacyProcessId, nameof(KitchenSink),
            KitchenSinkStreams.ReadyToClose(StreamLength).Cast<object>().ToArray());
        var withoutContext = await legacy.CreateService().GetProcessAsync(legacyProcessId, CancellationToken.None);
        Assert.True(withoutContext.Ok, withoutContext.Error?.Message);

        // The engine's answer must not change:
        Assert.Equal(
            withoutContext.Value!.NextSteps.Select(s => s.Name).OrderBy(x => x),
            withContext.Value!.NextSteps.Select(s => s.Name).OrderBy(x => x));
        Assert.True(legacy.Counters.EvaluatorReplays > 0, "legacy path should still hit the repository seam");
    }

    [Fact]
    public async Task Parity_execution_results_identical_with_and_without_context()
    {
        var processId = Guid.NewGuid();
        InnerStore.Seed(processId, nameof(KitchenSink),
            KitchenSinkStreams.ReadyToClose(StreamLength).Cast<object>().ToArray());
        var withContext = await CreateService().ExecuteCommandAsync(
            processId, nameof(CloseCase), null, null, CancellationToken.None);
        Assert.True(withContext.Ok, withContext.Error?.Message);

        using var legacy = new LegacyGraphFixture();
        var legacyProcessId = Guid.NewGuid();
        legacy.InnerStore.Seed(legacyProcessId, nameof(KitchenSink),
            KitchenSinkStreams.ReadyToClose(StreamLength).Cast<object>().ToArray());
        var withoutContext = await legacy.CreateService().ExecuteCommandAsync(
            legacyProcessId, nameof(CloseCase), null, null, CancellationToken.None);
        Assert.True(withoutContext.Ok, withoutContext.Error?.Message);

        Assert.Equal(
            withoutContext.Value!.NextSteps.Select(s => s.Name).OrderBy(x => x),
            withContext.Value!.NextSteps.Select(s => s.Name).OrderBy(x => x));
    }

    // ---- sharing: one rehydration per aggregate type per stream state ----

    [Fact]
    public async Task Get_process_rehydrates_each_aggregate_type_exactly_once()
    {
        var service = CreateService();
        var processId = Guid.NewGuid();
        InnerStore.Seed(processId, nameof(KitchenSink),
            KitchenSinkStreams.ReadyToClose(StreamLength).Cast<object>().ToArray());

        Counters.Reset();
        Metrics.Reset();
        var result = await service.GetProcessAsync(processId, CancellationToken.None);

        Assert.True(result.Ok, result.Error?.Message);
        Assert.Equal(0, Counters.EvaluatorReplays); // repository seam silent
        // One traversal touches exactly TWO aggregate types: KitchenSink (shared
        // by both conditionals) and ComplianceReview (the driven guest).
        // QuickAudit is NOT rehydrated: it lives in the Else branch, which loses
        // the conditional (Amount=50k) — losing-branch roots are never evaluated.
        Assert.Equal(2, Metrics.AggregateRehydrations);
        Assert.True(Metrics.TraversalMemoHits > 0, "branch traversals should be shared, not recomputed");
    }

    [Fact]
    public void Guest_aggregate_failure_is_cached_not_retried()
    {
        var metrics = new ReplayMetrics();
        var context = new ReplayContext(Registry, metrics);
        // Stream with NO QuickAudit events → TryAggregateAs semantics: failure.
        var stream = KitchenSinkStreams.ReadyToClose(50).Cast<object>().ToList();

        Assert.False(context.TryGetAggregate(typeof(QuickAudit), stream, out var first));
        Assert.False(context.TryGetAggregate(typeof(QuickAudit), stream, out var second));

        Assert.Null(first);
        Assert.Null(second);
        // The failed attempt replayed once and the FAILURE was cached — a second
        // ask must not retry the replay.
        Assert.Equal(1, metrics.AggregateRehydrations);
    }

    // ---- isolation: different stream states never share results ----

    [Fact]
    public void Different_stream_instances_get_separate_scopes()
    {
        var metrics = new ReplayMetrics();
        var context = new ReplayContext(Registry, metrics);

        var streamA = KitchenSinkStreams.ReadyToClose(50).Cast<object>().ToList();
        // Same CONTENT, different instance — must still be a separate scope,
        // because reference identity is the correctness boundary:
        var streamB = KitchenSinkStreams.ReadyToClose(50).Cast<object>().ToList();

        var a1 = (KitchenSink)context.GetAggregate(typeof(KitchenSink), streamA);
        var a2 = (KitchenSink)context.GetAggregate(typeof(KitchenSink), streamA);
        var b1 = (KitchenSink)context.GetAggregate(typeof(KitchenSink), streamB);

        Assert.Same(a1, a2);            // same list instance → shared aggregate
        Assert.NotSame(a1, b1);         // different instance → fresh rehydration
        Assert.Equal(2, metrics.AggregateRehydrations);
    }

    [Fact]
    public void Extended_stream_sees_new_state_not_stale_memo()
    {
        var context = new ReplayContext(Registry);

        var stream = KitchenSinkStreams.ReadyToClose(50).Cast<object>().ToList();
        var before = (KitchenSink)context.GetAggregate(typeof(KitchenSink), stream);
        Assert.False(before.Closed);

        // Post-dispatch reality: a NEW list with the appended event (SnapshotWith
        // builds a new list; nothing mutates in place). The context must see the
        // new state — this is the regression test for the old evaluators' latent
        // staleness bug.
        var extended = stream.Append((object)new CaseClosed()).ToList();
        var after = (KitchenSink)context.GetAggregate(typeof(KitchenSink), extended);
        Assert.True(after.Closed);
        Assert.False(before.Closed); // original scope untouched
    }

    /// <summary>Same graph/build as PerfGraphBase but WITHOUT a ReplayContext — the legacy path.</summary>
    private sealed class LegacyGraphFixture : PerfGraphBase
    {
        // PerfGraphBase wires the context; rebuilding definitions with a
        // context-free factory restores pre-1.2 evaluator behavior. The graph
        // must be rebuilt because nodes capture their evaluator factory.
        public LegacyGraphFixture()
        {
            var legacyFactory = new NodeEvaluatorFactory(Repository);
            RebuildDefinitions(legacyFactory);
        }

        // Protected members surfaced for the outer test class:
        public new InMemoryInstanceStore InnerStore => base.InnerStore;
        public new PerfCounters Counters => base.Counters;
        public new AgentProcessService CreateService() => base.CreateService();
    }
}
