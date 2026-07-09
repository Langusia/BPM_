using BPM.Tests.Application;
using Xunit;

namespace BPM.Tests.Perf;

/// <summary>
/// Phase 1 contract — these tests are RED against the current code by design.
/// They encode the Phase 1 exit criteria and go green when 1.1 (single load)
/// and 1.2 (shared replay context) land. Filter them out of CI until then:
///   dotnet test --filter "Category!=Phase1Contract"
///
/// Baseline behavior they document (current code):
///   - ExecuteCommandAsync loads the stream twice (before + after dispatch)
///   - every conditional/guest node evaluation replays the full stream
///     through IBpmRepository, once per node, per traversal
/// </summary>
[Trait("Category", "Phase1Contract")]
public class ReplayCountTests : PerfGraphBase
{
    private const int StreamLength = 200;

    [Fact]
    public async Task Execute_loads_the_stream_exactly_once()
    {
        var service = CreateService();
        var processId = Guid.NewGuid();
        InnerStore.Seed(processId, nameof(KitchenSink),
            KitchenSinkStreams.ReadyToClose(StreamLength).Cast<object>().ToArray());

        Counters.Reset();
        var result = await service.ExecuteCommandAsync(
            processId, nameof(CloseCase), argsJson: null, caller: null, CancellationToken.None);

        Assert.True(result.Ok, result.Error?.Message);
        // Phase 1.1 target — GREEN since the scoped event capture landed
        // (was 2: load before dispatch + reload after).
        Assert.Equal(1, Counters.StreamLoads);
    }

    [Fact]
    public async Task Execute_never_replays_the_stream_inside_evaluators()
    {
        var service = CreateService();
        var processId = Guid.NewGuid();
        InnerStore.Seed(processId, nameof(KitchenSink),
            KitchenSinkStreams.ReadyToClose(StreamLength).Cast<object>().ToArray());

        Counters.Reset();
        var result = await service.ExecuteCommandAsync(
            processId, nameof(CloseCase), argsJson: null, caller: null, CancellationToken.None);

        Assert.True(result.Ok, result.Error?.Message);
        // Phase 1.2 target — GREEN since ReplayContext landed: evaluators consume
        // the request's shared context, so the repository replay path is never
        // hit during traversal (was: 16 replays/execute on this fixture).
        Assert.Equal(0, Counters.EvaluatorReplays);
    }

    [Fact]
    public async Task Get_process_replays_at_most_once_per_stream_version()
    {
        var service = CreateService();
        var processId = Guid.NewGuid();
        InnerStore.Seed(processId, nameof(KitchenSink),
            KitchenSinkStreams.ReadyToClose(StreamLength).Cast<object>().ToArray());

        // Warm call at this stream version…
        _ = await service.GetProcessAsync(processId, CancellationToken.None);

        Counters.Reset();
        Metrics.Reset();
        // …then repeated reads at the SAME version must be served from the
        // version-keyed traversal cache (Phase 1.3): no replay work at all.
        // NOTE (post-1.2): this test counts IReplayMetrics.AggregateRehydrations,
        // not the repository seam — 1.2's ReplayContext silenced the seam
        // everywhere, but each fresh read still rehydrates each aggregate type
        // once (3/read on this fixture). 1.3's cross-request cache takes it to 0.
        for (var i = 0; i < 5; i++)
        {
            var result = await service.GetProcessAsync(processId, CancellationToken.None);
            Assert.True(result.Ok, result.Error?.Message);
        }

        Assert.Equal(0, Counters.EvaluatorReplays); // 1.2: repository seam silent
        Assert.Equal(0, Metrics.AggregateRehydrations); // 1.3 target: RED until then
    }

    /// <summary>
    /// Not a Phase 1 target — a fixture sanity check that must be GREEN now.
    /// If this fails, the graph or the stream generator is wrong, and every
    /// other number in this file is meaningless.
    /// </summary>
    [Fact]
    [Trait("Category", "FixtureSanity")]
    public async Task Fixture_sanity_close_case_is_the_single_remaining_mainline_step()
    {
        var service = CreateService();
        var processId = Guid.NewGuid();
        InnerStore.Seed(processId, nameof(KitchenSink),
            KitchenSinkStreams.ReadyToClose(StreamLength).Cast<object>().ToArray());

        var result = await service.GetProcessAsync(processId, CancellationToken.None);

        Assert.True(result.Ok, result.Error?.Message);
        var names = result.Value!.NextSteps.Select(c => c.Name).ToList();
        Assert.Contains(nameof(CloseCase), names);
    }
}
