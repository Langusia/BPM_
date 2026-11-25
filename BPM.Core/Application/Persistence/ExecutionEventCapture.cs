using System;
using System.Collections.Generic;

namespace BPM.Core.Application.Persistence;

/// <summary>
/// Per-request capture of the events appended during command dispatch
/// (OPTIMIZE Phase 1.1: single stream load per execute).
///
/// The dispatch pipeline itself performs every process-event write, through
/// <see cref="BPM.Core.Process.IProcessStore"/>. This capture keeps an
/// in-memory copy of exactly those events so the execution result can be
/// built from (pre-dispatch snapshot + appended events) instead of reloading
/// the whole stream from storage.
///
/// INVARIANT: <see cref="BPM.Core.Process.IProcessStore"/> is the only
/// sanctioned write path for process events. Any code that appends process
/// events around it makes results derived from this capture stale.
///
/// Register SCOPED — one instance per request/execution. The reader
/// (<see cref="BPM.Core.Application.Execution.AgentProcessService"/>) and the
/// writer (<see cref="BPM.Core.Process.ProcessStore"/>) must resolve from the
/// same scope, otherwise the capture reads empty and the service falls back
/// to a reload (logged as a warning).
/// </summary>
public interface IExecutionEventCapture
{
    /// <summary>Records events appended for a process. Called by the write path just before commit.</summary>
    void Record(Guid processId, IReadOnlyList<object> events);

    /// <summary>Returns the captured events for the process and clears them (a second call returns empty).</summary>
    IReadOnlyList<object> TakeFor(Guid processId);
}

/// <summary>Default implementation. Not thread-safe by design: scoped lifetime, single request.</summary>
public sealed class ExecutionEventCapture : IExecutionEventCapture
{
    private readonly Dictionary<Guid, List<object>> _byProcess = new();

    public void Record(Guid processId, IReadOnlyList<object> events)
    {
        if (events.Count == 0)
            return;
        if (!_byProcess.TryGetValue(processId, out var list))
            _byProcess[processId] = list = [];
        list.AddRange(events);
    }

    public IReadOnlyList<object> TakeFor(Guid processId) =>
        _byProcess.Remove(processId, out var list) ? list : Array.Empty<object>();
}
