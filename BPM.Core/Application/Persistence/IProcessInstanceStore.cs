using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BPM.Core.Application.Persistence;

/// <summary>
/// Narrow persistence port for the application layer (catalog, projection,
/// execute + next-steps). Read-side only: writes flow through the command
/// handlers and <see cref="BPM.Core.Process.IProcessStore"/>. No storage types
/// (Marten or otherwise) appear in this surface; a second storage provider is
/// an additive package implementing this port.
/// </summary>
public interface IProcessInstanceStore
{
    /// <summary>
    /// Loads a process instance's full event stream, or null when no stream
    /// exists for the id.
    /// </summary>
    Task<ProcessInstanceSnapshot?> LoadAsync(Guid processId, CancellationToken ct);
}

/// <summary>A process instance as loaded from storage: plain event data plus stream metadata.</summary>
public sealed record ProcessInstanceSnapshot(
    Guid ProcessId,
    string AggregateTypeName,
    IReadOnlyList<ProcessEventEnvelope> Events,
    DateTimeOffset? StartedAt)
{
    /// <summary>
    /// False when event versions were synthesized in memory (post-dispatch
    /// SnapshotWith, Phase 1.1) rather than loaded from the store. Snapshots
    /// with non-authoritative versions are invisible to the version-keyed
    /// traversal cache (Phase 1.3) — they can neither serve nor populate it.
    /// </summary>
    public bool VersionsAuthoritative { get; init; } = true;
}

/// <summary>One stored event: deserialized event data plus stream position and timestamp.</summary>
public sealed record ProcessEventEnvelope(
    string EventTypeName,
    object Data,
    long Version,
    DateTimeOffset Timestamp);
