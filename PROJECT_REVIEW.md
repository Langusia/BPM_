# BPM.Core — Project Review & Roadmap

*Review date: 2026-03-07*

---

## 1. Project Summary

**BPM.Core** is a lightweight, code-first Business Process Management engine for .NET. It lets developers define stateful workflows as directed graphs using a fluent builder API, execute them step-by-step with event sourcing (via Marten/PostgreSQL), and enforce ordering, branching, and validation at runtime.

| Metric | Value |
|--------|-------|
| NuGet Package | `BPM_.Core` v3.0.1 |
| Target Frameworks | .NET 8.0 / 9.0 / 10.0 |
| Core LOC (C#) | ~2,360 lines across 62 files |
| Test Count | 18 unit tests (1 test file) |
| Contributors | 4 |
| Total Commits | 77 |
| License | MIT |

### Solution Structure

| Project | Purpose |
|---------|---------|
| **BPM.Core** | The NuGet library — graph builder, node evaluators, process store, event sourcing |
| **BPM.UI** | ASP.NET Core companion package — auto-generated interactive process canvas (React Flow + Dagre) |
| **BPM.Client** | Demo/sample API app (OrderFulfillment, UserRegistration, XProcess workflows) |
| **BPM.Tests** | xUnit tests for graph configuration |

---

## 2. What Exists Today (Strengths)

### Core Engine
- **Rich fluent builder API** — `StartWith`, `Continue`, `Or`, `If/Else`, `Group`, `UnlockOptional`, `ContinueAnyTime`, `JumpTo` (guest processes). This is a genuinely expressive DSL.
- **Event sourcing** baked in via Marten — processes are fully replayable.
- **Node evaluation pipeline** — dedicated evaluators for each node type (conditional, group, optional, anytime, guest process).
- **Validation** — `Validate<TCommand>()` and `GetNextSteps()` enforce the graph at runtime.
- **Process expiration** — configurable TTL on processes.
- **MediatR integration** — commands are standard `IRequest` types; only depends on `MediatR.Contracts` (MIT, no licensing issues).
- **Multi-target NuGet** — properly packaged with logo, README, and metadata.

### UI Package
- Auto-generates an interactive graph visualization from process definitions.
- React Flow + Dagre layout with custom node types (standard, group, conditional, jump-to).
- Convention-based endpoint mapping with fluent configuration.
- Vite-powered frontend dev server integration.

### Infrastructure
- Docker Compose for PostgreSQL.
- Dockerfile for the sample client app.
- Scalar API reference (OpenAPI).

### Documentation
- Comprehensive README with quickstart, all builder features, complex branching example, and full API reference table.

---

## 3. What's Missing / Weaknesses

### 3.1 Testing (Critical Gap)

| Issue | Detail |
|-------|--------|
| **Only 18 tests in 1 file** | Only `BProcessGraphConfiguration` is tested. No tests for process execution, event appending, validation, the store, or node evaluation. |
| **No integration tests** | Nothing verifies the Marten persistence path end-to-end. |
| **No UI tests** | Zero frontend tests. |
| **No test coverage reporting** | No tooling to measure or enforce coverage. |

### 3.2 CI/CD (Non-existent)

- **No GitHub Actions / CI pipeline** — no automated build, test, lint, or NuGet publish workflow.
- **No branch protection** or automated PR checks.
- No automated NuGet publishing on tag/release.

### 3.3 Error Handling & Observability

- No structured logging beyond basic `ILogger` injection. No correlation IDs, no process-level tracing.
- Custom exceptions exist but no global error-handling middleware in the sample app.
- No metrics or health check endpoints.

### 3.4 Process Features

| Missing Feature | Impact |
|-----------------|--------|
| **Process versioning / migration** | No way to evolve a process definition after instances exist. Breaking change = broken running processes. |
| **Retry / compensation** | No built-in retry policy or saga-style compensation for failed steps. |
| **Timeouts per step** | Only process-level expiration; no per-node deadlines. |
| **Process history / audit trail API** | Events exist but no queryable history endpoint or projection. |
| **Bulk operations** | No batch fetch/query of processes by status, type, date range. |
| **Webhooks / notifications** | No callback mechanism when a process reaches a certain step or completes. |
| **Sub-process data passing** | JumpTo exists but data exchange between parent and guest processes is unclear. |

### 3.5 UI Package

- No published NuGet package metadata (version, description only — no actual publish).
- No authentication/authorization on the UI dashboard.
- No process instance visualization (only shows the graph definition, not live process state).
- No ability to manually advance or retry steps from the UI.
- Frontend uses JSX (not TypeScript) — no type safety.

### 3.6 Documentation

- No architecture diagram or design doc explaining internals.
- No CONTRIBUTING.md or developer setup guide.
- No CHANGELOG.
- No XML doc comments on public APIs (affects IntelliSense for NuGet consumers).

### 3.7 Code Quality

- Static mutable state in `BProcessGraphConfiguration._processes` — requires reflection to reset in tests. Thread-safety concern.
- No `.editorconfig` or code style enforcement.
- No static analysis (Roslyn analyzers, SonarQube, etc.).

---

## 4. Suggested Roadmap

### Phase 1 — Foundation (Quality & CI)

1. **Set up GitHub Actions CI pipeline**
   - Build + test on PR for all three target frameworks.
   - NuGet pack validation.
   - Automated NuGet publish on version tag.

2. **Expand test coverage to ≥70%**
   - Unit tests for: `Process`, `ProcessStore`, all node evaluators, `BpmResult`, validation logic.
   - Integration tests with a real PostgreSQL (Testcontainers).
   - Add `coverlet` for coverage reporting.

3. **Add XML doc comments** to all public types and methods in BPM.Core.

4. **Add `.editorconfig`** and enforce formatting via CI.

5. **Add CHANGELOG.md** and start semantic versioning discipline.

### Phase 2 — Core Engine Enhancements

6. **Process versioning** — allow definitions to evolve with a version number; running instances continue on their original version.

7. **Per-step timeouts** — extend the builder API: `.Continue<T>(timeout: TimeSpan)`.

8. **Retry policies** — configurable retry count/backoff per node for transient failures.

9. **Compensation / rollback steps** — builder API for declaring compensating actions on failure.

10. **Process querying** — add `IProcessQueryService` with filters (status, aggregate type, date range, custom predicates).

11. **Refactor static configuration** — replace `BProcessGraphConfiguration._processes` static dictionary with a scoped/singleton service registered in DI. Eliminates thread-safety issues and reflection hacks in tests.

### Phase 3 — Observability & Operations

12. **Structured logging** with correlation IDs per process instance.

13. **Process audit trail endpoint** — expose the event stream for a process as a queryable timeline.

14. **Health checks** — ASP.NET Core health check for PostgreSQL + Marten connectivity.

15. **Metrics** — OpenTelemetry counters for processes started, completed, failed, expired.

16. **Webhook / event notifications** — allow registering callbacks on process lifecycle events.

### Phase 4 — UI & Developer Experience

17. **Live process instance view** — show current step, completed steps, available next steps for a running process.

18. **Manual intervention UI** — ability to advance, retry, or fail a process step from the dashboard.

19. **Migrate frontend to TypeScript** for type safety.

20. **Auth on UI** — at minimum, restrict the dashboard to authenticated users.

21. **Publish BPM_.UI as a NuGet package** with proper versioning.

22. **Add a `dotnet new` template** — scaffold a new BPM project with a single command.

### Phase 5 — Advanced Features

23. **Saga / distributed transaction support** — coordinate across multiple aggregates with compensating actions.

24. **Process instance migration tool** — when definitions change, provide a migration path for in-flight processes.

25. **Plugin system** — allow custom node types via an extensibility point.

26. **Multi-tenancy** — tenant-scoped process stores for SaaS scenarios.

27. **Performance benchmarks** — BenchmarkDotNet suite for process creation, event appending, and graph evaluation.

---

## 5. Priority Matrix

| Priority | Item | Effort | Impact |
|----------|------|--------|--------|
| **P0** | CI/CD pipeline | Small | High |
| **P0** | Expand test coverage | Medium | High |
| **P1** | XML doc comments | Small | Medium |
| **P1** | Refactor static config to DI | Small | Medium |
| **P1** | Process querying API | Medium | High |
| **P2** | Process versioning | Large | High |
| **P2** | Live process instance UI | Medium | High |
| **P2** | Structured logging + correlation | Small | Medium |
| **P3** | Per-step timeouts + retries | Medium | Medium |
| **P3** | TypeScript migration (UI) | Medium | Low |
| **P3** | Compensation / sagas | Large | Medium |

---

*This review is based on the repository state as of commit `0b05b16` (master).*
