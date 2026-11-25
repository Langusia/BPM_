using Xunit;

namespace BPM.Tests.Perf;

/// <summary>
/// Phase 1.1 behavior tests — GREEN from the moment 1.1 lands (not part of the
/// red-by-design Phase1Contract set). They pin the capture contract:
/// the execution result reflects the events the dispatch appended, without a
/// second stream load.
/// </summary>
public class EventCaptureTests : PerfGraphBase
{
    [Fact]
    public async Task Execute_result_reflects_captured_events_without_reload()
    {
        var service = CreateService();
        var processId = Guid.NewGuid();
        InnerStore.Seed(processId, nameof(KitchenSink),
            KitchenSinkStreams.ReadyToClose(50).Cast<object>().ToArray());

        Counters.Reset();
        var result = await service.ExecuteCommandAsync(
            processId, nameof(CloseCase), argsJson: null, caller: null, CancellationToken.None);

        Assert.True(result.Ok, result.Error?.Message);
        // Single load: availability check only; the result was synthesized from
        // snapshot + captured CaseClosed, never re-read from the store.
        Assert.Equal(1, Counters.StreamLoads);
        // And the synthesized state is genuinely post-command: the process is
        // closed, so CloseCase must no longer be offered as a next step.
        Assert.DoesNotContain(nameof(CloseCase), result.Value!.NextSteps.Select(s => s.Name));
    }

    [Fact]
    public async Task Execute_with_no_appended_events_reuses_the_snapshot()
    {
        var service = CreateService();
        var processId = Guid.NewGuid();
        InnerStore.Seed(processId, nameof(KitchenSink),
            KitchenSinkStreams.ReadyToClose(50).Cast<object>().ToArray());

        // Suppress event production entirely: dispatch succeeds but appends nothing.
        Dispatcher.OnDispatch = _ => Task.FromResult<object?>(null);

        Counters.Reset();
        var result = await service.ExecuteCommandAsync(
            processId, nameof(CloseCase), argsJson: null, caller: null, CancellationToken.None);

        Assert.True(result.Ok, result.Error?.Message);
        // Empty capture + producer-declaring command → scope-mismatch guard
        // kicks in: one reload for correctness (2 loads total), plus a warning.
        // This documents the guard; the happy path is the test above.
        Assert.Equal(2, Counters.StreamLoads);
    }
}
