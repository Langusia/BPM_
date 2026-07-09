using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace BPM.Core.Application.Execution;

/// <summary>
/// OPTIMIZE Phase 1.3 — cross-request, version-keyed cache of the root traversal
/// result (the available command types for a process instance).
///
/// Correctness model: entries are keyed by (processType, processId, streamVersion)
/// where the version comes from the STORE (snapshots with synthesized versions —
/// <see cref="BPM.Core.Application.Persistence.ProcessInstanceSnapshot.VersionsAuthoritative"/>
/// == false — never touch the cache). Any new event bumps the stream version, so a
/// changed process simply misses and recomputes: there is no invalidation logic to
/// get wrong, and mid-process changes to condition inputs (e.g. an amount) are
/// handled by construction.
///
/// Register SINGLETON — it is cross-request by definition. Replaces the dead,
/// data-blind NodeBase._cache (deleted in the same change).
/// </summary>
public interface ITraversalResultCache
{
    bool TryGet(string processType, Guid processId, long version, out IReadOnlyList<Type> commandTypes);
    void Set(string processType, Guid processId, long version, IReadOnlyList<Type> commandTypes);
}

/// <summary>
/// Self-contained bounded implementation (no external caching dependency).
/// Keeps ONE entry per process instance — the latest version seen. The agent
/// always reads the stream head, so caching superseded versions buys nothing;
/// overwriting on version change doubles as cleanup.
/// </summary>
public sealed class TraversalResultCache(int maxEntries = 10_000) : ITraversalResultCache
{
    private sealed record Entry(string ProcessType, long Version, IReadOnlyList<Type> CommandTypes, long Stamp);

    private readonly ConcurrentDictionary<Guid, Entry> _entries = new();
    private long _stamp;

    public bool TryGet(string processType, Guid processId, long version, out IReadOnlyList<Type> commandTypes)
    {
        if (_entries.TryGetValue(processId, out var entry)
            && entry.Version == version
            && entry.ProcessType == processType)
        {
            commandTypes = entry.CommandTypes;
            return true;
        }

        commandTypes = Array.Empty<Type>();
        return false;
    }

    public void Set(string processType, Guid processId, long version, IReadOnlyList<Type> commandTypes)
    {
        _entries[processId] = new Entry(processType, version, commandTypes,
            System.Threading.Interlocked.Increment(ref _stamp));

        if (_entries.Count <= maxEntries)
            return;

        // Coarse bound: evict the oldest-stamped quarter. Cheap, rare, and only a
        // cache — a wrongly evicted entry costs one recompute, never correctness.
        foreach (var victim in _entries.ToArray()
                     .OrderBy(kv => kv.Value.Stamp)
                     .Take(Math.Max(1, maxEntries / 4)))
            _entries.TryRemove(victim.Key, out _);
    }
}
