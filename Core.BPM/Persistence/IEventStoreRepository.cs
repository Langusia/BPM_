using Marten.Events;

namespace Core.BPM.Persistence;

public interface IEventStoreRepository
{
    Task<IReadOnlyList<IEvent>> FetchStreamAsync(Guid id, CancellationToken ct);
    Task<T?> AggregateStreamAsync<T>(Guid id, CancellationToken ct) where T : Aggregate;
    Task AppendEvents(Guid aggregateId, object[] events, bool newStream = true, Dictionary<string, object>? headers = null, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}