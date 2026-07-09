using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Marten;

namespace BPM.Core.Application.Persistence;

/// <summary>
/// Marten implementation of the application-layer persistence port. The only
/// place the application layer touches Marten; sessions stay internal.
/// </summary>
internal sealed class MartenProcessInstanceStore(IQuerySession session) : IProcessInstanceStore
{
    public async Task<ProcessInstanceSnapshot?> LoadAsync(Guid processId, CancellationToken ct)
    {
        var stream = await session.Events.FetchStreamAsync(processId, token: ct);
        if (stream is null || stream.Count == 0)
            return null;

        var first = stream[0];
        var aggregateTypeName = first.Headers?.TryGetValue("AggregateType", out var header) == true
            ? header?.ToString()
            : null;
        if (string.IsNullOrEmpty(aggregateTypeName))
            return null;

        var envelopes = stream
            .Select(e => new ProcessEventEnvelope(e.Data.GetType().Name, e.Data, e.Version, e.Timestamp))
            .ToList();

        return new ProcessInstanceSnapshot(processId, aggregateTypeName, envelopes, first.Timestamp);
    }
}
