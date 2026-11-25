using BPM.Core.Application.Execution;
using Xunit;

namespace BPM.Tests.Perf;

/// <summary>
/// Phase 1.3 behavior tests — GREEN from the moment the traversal cache lands.
/// The critical ones are the staleness guards: a cached result must never
/// survive a state change, and synthesized (post-dispatch) snapshots must be
/// invisible to the cache entirely.
/// </summary>
public class TraversalCacheTests : PerfGraphBase
{
    private const int StreamLength = 100;

    [Fact]
    public async Task Cached_read_returns_identical_result_with_zero_work()
    {
        var service = CreateService();
        var processId = Guid.NewGuid();
        InnerStore.Seed(processId, nameof(KitchenSink),
            KitchenSinkStreams.ReadyToClose(StreamLength).Cast<object>().ToArray());

        var first = await service.GetProcessAsync(processId, CancellationToken.None);
        Assert.True(first.Ok, first.Error?.Message);

        Counters.Reset();
        Metrics.Reset();
        var second = await service.GetProcessAsync(processId, CancellationToken.None);

        Assert.True(second.Ok, second.Error?.Message);
        // Identical answer...
        Assert.Equal(
            first.Value!.NextSteps.Select(s => s.Name).OrderBy(x => x),
            second.Value!.NextSteps.Select(s => s.Name).OrderBy(x => x));
        // ...with zero traversal work:
        Assert.Equal(0, Counters.EvaluatorReplays);
        Assert.Equal(0, Metrics.AggregateRehydrations);
    }

    [Fact]
    public async Task New_events_invalidate_by_version_no_stale_serve()
    {
        var service = CreateService();
        var processId = Guid.NewGuid();
        InnerStore.Seed(processId, nameof(KitchenSink),
            KitchenSinkStreams.ReadyToClose(StreamLength).Cast<object>().ToArray());

        // Warm the cache at the pre-close version:
        var before = await service.GetProcessAsync(processId, CancellationToken.None);
        Assert.Contains(nameof(CloseCase), before.Value!.NextSteps.Select(s => s.Name));

        // State changes: CaseClosed lands in the STORE (version bumps).
        var closed = new CaseClosed { NodeId = LevelOfCloseCase() };
        InnerStore.Seed(processId, nameof(KitchenSink), closed);

        // Read again: MUST reflect the new state, not the cached pre-close result.
        var after = await service.GetProcessAsync(processId, CancellationToken.None);
        Assert.True(after.Ok, after.Error?.Message);
        Assert.DoesNotContain(nameof(CloseCase), after.Value!.NextSteps.Select(s => s.Name));
    }

    [Fact]
    public async Task Synthesized_snapshots_never_touch_the_cache()
    {
        var service = CreateService();
        var processId = Guid.NewGuid();
        InnerStore.Seed(processId, nameof(KitchenSink),
            KitchenSinkStreams.ReadyToClose(StreamLength).Cast<object>().ToArray());

        // Execute: pre-dispatch check caches at the store version (N); the
        // post-dispatch traversal runs on a synthesized snapshot claiming a
        // higher version. If that synthesized result leaked into the cache, the
        // NEXT read after a real store append at that version would serve wrong data.
        var executed = await service.ExecuteCommandAsync(
            processId, nameof(CloseCase), null, null, CancellationToken.None);
        Assert.True(executed.Ok, executed.Error?.Message);
        // The execution result itself reflects post-close state (capture path):
        Assert.DoesNotContain(nameof(CloseCase), executed.Value!.NextSteps.Select(s => s.Name));

        // Store is UNCHANGED (FakeDispatcher doesn't persist) — same version as
        // before the execute. A read must serve the PRE-close state (CloseCase
        // still available), proving the synthesized post-close result did not
        // poison the version-keyed entry.
        var read = await service.GetProcessAsync(processId, CancellationToken.None);
        Assert.Contains(nameof(CloseCase), read.Value!.NextSteps.Select(s => s.Name));
    }

    [Fact]
    public async Task Distinct_processes_with_equal_versions_do_not_collide()
    {
        var service = CreateService();
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        // Same type, same event count (=> same version number), different content:
        InnerStore.Seed(p1, nameof(KitchenSink),
            KitchenSinkStreams.ReadyToClose(StreamLength).Cast<object>().ToArray());
        var shorter = KitchenSinkStreams.ReadyToClose(StreamLength).Take(3).Cast<object>().ToList();
        while (shorter.Count < StreamLength)
            shorter.Add(shorter[^1]);
        InnerStore.Seed(p2, nameof(KitchenSink), shorter.ToArray());

        var r1 = await service.GetProcessAsync(p1, CancellationToken.None);
        var r2 = await service.GetProcessAsync(p2, CancellationToken.None);

        Assert.True(r1.Ok && r2.Ok);
        // p1 is ready to close; p2 stalled early — results must differ despite
        // identical (type, version) because processId is part of the key.
        Assert.Contains(nameof(CloseCase), r1.Value!.NextSteps.Select(s => s.Name));
        Assert.DoesNotContain(nameof(CloseCase), r2.Value!.NextSteps.Select(s => s.Name));
    }

    [Fact]
    public void Bound_evicts_without_breaking_correctness()
    {
        var cache = new TraversalResultCache(maxEntries: 8);
        var types = new List<Type> { typeof(CloseCase) };

        var ids = Enumerable.Range(0, 12).Select(_ => Guid.NewGuid()).ToList();
        foreach (var id in ids)
            cache.Set(nameof(KitchenSink), id, 1, types);

        // Some were evicted (bound respected)...
        var surviving = ids.Count(id => cache.TryGet(nameof(KitchenSink), id, 1, out _));
        Assert.True(surviving < ids.Count);
        // ...and whatever survived answers correctly:
        foreach (var id in ids)
            if (cache.TryGet(nameof(KitchenSink), id, 1, out var hit))
                Assert.Equal(types, hit);
        // Version mismatch is always a miss:
        Assert.False(cache.TryGet(nameof(KitchenSink), ids[^1], 2, out _));
    }

    private static int LevelOfCloseCase()
    {
        var config = BPM.Core.Configuration.BProcessGraphConfiguration.GetConfig(nameof(KitchenSink))!;
        return KitchenSinkStreams.DeepNodes(config.RootNode, new HashSet<BPM.Core.Nodes.INode>())
            .First(n => n.CommandType == typeof(CloseCase)).NodeLevel;
    }
}
