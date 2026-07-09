using System.Diagnostics;
using Xunit;

namespace BPM.Tests.Perf;

/// <summary>
/// Latency baseline for the OPTIMIZE roadmap. Run BEFORE starting Phase 1 and
/// after each phase item, on the same machine, and record the numbers:
///
///   dotnet test --filter "Category=PerfBaseline" --logger "console;verbosity=detailed"
///
/// Deliberately runs on the in-memory store: this isolates ENGINE cost
/// (traversal, evaluators, replays) from Postgres I/O. The Phase 1 exit
/// assertion (p95 &lt; 50ms at 1000 events) stays commented out until the
/// phase lands — until then this test only reports.
/// </summary>
[Trait("Category", "PerfBaseline")]
public class ExecuteLatencyBench : PerfGraphBase
{
    private const int Warmup = 20;
    private const int Iterations = 200;

    [Theory]
    [InlineData(50)]
    [InlineData(200)]
    [InlineData(1000)]
    public async Task Execute_latency_baseline(int streamLength)
    {
        var service = CreateService();
        var processId = Guid.NewGuid();
        InnerStore.Seed(processId, nameof(KitchenSink),
            KitchenSinkStreams.ReadyToClose(streamLength).Cast<object>().ToArray());

        // Warmup: JIT, allocations, evaluator paths.
        for (var i = 0; i < Warmup; i++)
        {
            var warm = await service.ExecuteCommandAsync(
                processId, nameof(CloseCase), null, null, CancellationToken.None);
            Assert.True(warm.Ok, warm.Error?.Message);
        }

        Counters.Reset();
        Metrics.Reset();
        var timings = new double[Iterations];
        var sw = new Stopwatch();
        for (var i = 0; i < Iterations; i++)
        {
            sw.Restart();
            _ = await service.ExecuteCommandAsync(
                processId, nameof(CloseCase), null, null, CancellationToken.None);
            sw.Stop();
            timings[i] = sw.Elapsed.TotalMilliseconds;
        }

        Array.Sort(timings);
        var p50 = timings[(int)(Iterations * 0.50)];
        var p95 = timings[(int)(Iterations * 0.95)];
        var max = timings[^1];
        var loadsPerCall = Counters.StreamLoads / (double)Iterations;
        var replaysPerCall = Counters.EvaluatorReplays / (double)Iterations;
        var rehydrationsPerCall = Metrics.AggregateRehydrations / (double)Iterations;

        // xunit.v3 surfaces TestContext output; also write to console for CI logs.
        var report =
            $"[KitchenSink execute | {streamLength} events] " +
            $"p50={p50:F3}ms p95={p95:F3}ms max={max:F3}ms | " +
            $"loads/call={loadsPerCall:F1} evaluatorReplays/call={replaysPerCall:F1} " +
            $"rehydrations/call={rehydrationsPerCall:F1}";
        TestContext.Current.TestOutputHelper?.WriteLine(report);
        Console.WriteLine(report);

        // Phase 1 exit assertion — ACTIVE since 1.1–1.3 landed:
        if (streamLength == 1000)
            Assert.True(p95 < 50, $"Phase 1 exit: p95 {p95:F3}ms >= 50ms");
    }

    [Theory]
    [InlineData(1000)]
    public async Task Repeated_reads_latency_baseline(int streamLength)
    {
        var service = CreateService();
        var processId = Guid.NewGuid();
        InnerStore.Seed(processId, nameof(KitchenSink),
            KitchenSinkStreams.ReadyToClose(streamLength).Cast<object>().ToArray());

        for (var i = 0; i < Warmup; i++)
            _ = await service.GetProcessAsync(processId, CancellationToken.None);

        Counters.Reset();
        Metrics.Reset();
        var timings = new double[Iterations];
        var sw = new Stopwatch();
        for (var i = 0; i < Iterations; i++)
        {
            sw.Restart();
            _ = await service.GetProcessAsync(processId, CancellationToken.None);
            sw.Stop();
            timings[i] = sw.Elapsed.TotalMilliseconds;
        }

        Array.Sort(timings);
        var report =
            $"[KitchenSink get_process | {streamLength} events, same version] " +
            $"p50={timings[(int)(Iterations * 0.50)]:F3}ms " +
            $"p95={timings[(int)(Iterations * 0.95)]:F3}ms " +
            $"max={timings[^1]:F3}ms | " +
            $"evaluatorReplays/call={Counters.EvaluatorReplays / (double)Iterations:F1} " +
            $"rehydrations/call={Metrics.AggregateRehydrations / (double)Iterations:F1}";
        TestContext.Current.TestOutputHelper?.WriteLine(report);
        Console.WriteLine(report);
        // After Phase 1.3 this becomes a cache hit: expect ~0 replays and sub-ms p95.
    }
}
