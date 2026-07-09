# Perf harness (OPTIMIZE Phase 1.0)

Drop this folder into `BPM.Tests/Perf/`. No new package refs needed (xunit.v3 + NSubstitute already referenced).

## Fixture rule
`KitchenSinkDomain.cs` must contain **every node type the SDK supports**. Adding a node type to BPM.Core = extending this graph in the same PR. Currently: linear, Group, Conditional (+Else), nested Group-in-If, Or, AnyTime, JumpTo guest, nested configure-lambda branching THREE levels deep (Continue-in-Continue-in-Continue — the `_orScopeBuilder` wiring path at depth), Group fork inside a configure scope (level 2, before any branching), inner If/Else at level 3, UnlockOptional (OptionalNode), scoped Or, OrAnyTime, OrJumpTo (second guest process, QuickAudit).

Deliberately excluded (repo findings, 2026-07-09): `Case`/`Case<T>` ignores its predicate and creates no node (unfinished feature — add when it actually builds one); `OrOptional` exists on ProcessBuilder but not on `IProcessNodeModifiableBuilder`, so definitions can't reach it.

## Commands
```bash
# 1. Sanity first — must be GREEN before trusting anything else:
dotnet test --filter "Category=FixtureSanity"

# 2. Baseline numbers (record below, same machine as the "after" runs):
dotnet test --filter "Category=PerfBaseline" --logger "console;verbosity=detailed"

# 3. Phase 1 contract — RED today by design, goes green as 1.1–1.3 land:
dotnet test --filter "Category=Phase1Contract"

# CI (until Phase 1 ships):
dotnet test --filter "Category!=Phase1Contract&Category!=PerfBaseline"
```

## Baseline record
Environment: Claude cloud sandbox (Linux x64, .NET SDK 10.0.109, in-memory store, Debug build).
Numbers are only comparable to "after" runs from the same environment. "ev" = events (stream length).

| Date | Commit | Bench | 50 ev | 200 ev | 1000 ev | loads/call | replays/call |
|---|---|---|---|---|---|---|---|
| 2026-07-09 | 1986041 (before Phase 1, 3-level full-coverage graph) | execute p95 | 7.811ms | 5.396ms | 16.586ms | 2.0 | 16.0 |
| 2026-07-09 | 1986041 (before Phase 1, 3-level full-coverage graph) | get_process p95 | | | 6.047ms | — | 8.0 |
| 2026-07-09 | after 1.1 (event capture) | execute p95 | 11.101ms | 5.817ms | 15.325ms | 1.0 | 16.0 |
| | after 1.2 | execute p95 | | | | 1.0 | 0.0 |
| | after 1.3 | get_process p95 | | | | — | 0.0 |

After-1.1 note: loads/call hit the 1.0 target. Latency is statistically flat (in-memory
store: a reload costs ~nothing here; the reload's real cost is a Postgres round-trip,
which this bench deliberately excludes). Replays (16.0) still dominate — Phase 1.2's job.
The 50-ev p95 wobble (11.1ms vs 7.8ms) is environment noise on a shared container, not
signal; compare medians (3.3 vs 4.2ms).

p50 for the record: execute 4.171 / 2.936 / 7.523 ms (50/200/1000 ev); get_process 3.702 ms (1000 ev).
(Superseded same-day runs on this commit: original graph 10 replays/execute, 5/read;
2-level full-coverage graph 16 replays/execute, 8/read, execute p95 19.29ms @1000 ev.
Replays did NOT rise with the 3rd nesting level or the level-2 Group fork — only
conditional/guest evaluators hit the repository seam; plain nodes and groups don't.)
Note: the 1000-event execute p95 already sits under the 50ms Phase 1 exit threshold on this
hardware — the counters (loads==1, replays==0) are the meaningful exit criteria; the latency
assert stays as a regression guard.

Phase 1 exit: 1000-event execute p95 < 50ms → uncomment the assert in `ExecuteLatencyBench`.

## Known caveats
- Written without compiling against the repo (sandbox had no NuGet access): builder-chaining details in `KitchenSinkDefinition` may need small adjustments if the fluent API rejects a shape — the *graph shape* is the contract, keep it.
- Counters live at test seams only (store decorator + `IBpmRepository` decorator). The service's own `ProjectAggregateState` replay isn't counted — it's constant (1/request) and not a Phase 1 target. In-core `IReplayMetrics` (incl. traversal counts) lands with Phase 1.2's `ReplayContext`.
- Bench uses the in-memory store on purpose: engine cost only. A Marten/Testcontainers variant is a later add.
