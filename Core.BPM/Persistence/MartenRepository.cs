using Marten;
using Marten.Events;

namespace Core.BPM.Persistence;

public class MartenRepository<T> where T : Aggregate
{
    private readonly IDocumentSession _documentSession;

    public MartenRepository(IDocumentSession documentSession)
    {
        _documentSession = documentSession;
    }

    public Task<T?> Find(Guid id, CancellationToken ct) =>
        _documentSession.Events.AggregateStreamAsync<T>(id, token: ct);

    public Task<IReadOnlyList<IEvent>> FetchStreamAsync(Guid id, CancellationToken ct) =>
        _documentSession.Events.FetchStreamAsync(id, token: ct);

    public async Task<long> Add(T aggregate, CancellationToken ct = default)
    {
        var events = aggregate.DequeueUncommittedEvents();
        _documentSession.Events.StartStream<T>(
            aggregate.Id,
            events
        );

        await _documentSession.SaveChangesAsync(ct).ConfigureAwait(false);

        return events.Length;
    }

    public async Task<long> Update(T aggregate, long? expectedVersion = null, CancellationToken ct = default)
    {
        var events = aggregate.DequeueUncommittedEvents();
        var nextVersion = (expectedVersion ?? aggregate.Version) + events.Length;

        _documentSession.Events.Append(
            aggregate.Id,
            nextVersion,
            events
        );

        await _documentSession.SaveChangesAsync(ct).ConfigureAwait(false);

        return nextVersion;
    }


    public Task<long> Delete(T aggregate, long? expectedVersion = null, CancellationToken ct = default) =>
        Update(aggregate, expectedVersion, ct);
}

public class MartenRepository
{
    private readonly IDocumentSession _documentSession;

    public MartenRepository(IDocumentSession documentSession) =>
        _documentSession = documentSession;

    public Task<IReadOnlyList<IEvent>> FetchStreamAsync(Guid id, CancellationToken ct) =>
        _documentSession.Events.FetchStreamAsync(id, token: ct);

    public async Task<long> Update(Aggregate aggregate, long? expectedVersion = null, CancellationToken ct = default)
    {
        var s = await _documentSession.Events.FetchStreamStateAsync(aggregate.Id);
        var events = aggregate.DequeueUncommittedEvents();

        var nextVersion = (expectedVersion ?? aggregate.Version) + events.Length;

        _documentSession.Events.Append(
            aggregate.Id,
            nextVersion,
            events
        );

        await _documentSession.SaveChangesAsync(ct).ConfigureAwait(false);

        return nextVersion;
    }
}