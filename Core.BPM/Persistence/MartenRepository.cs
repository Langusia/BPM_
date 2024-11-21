using Core.BPM.Registry;
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
    private readonly IQuerySession _querySession;
    private readonly ProcessRegistry _registry;

    public MartenRepository(IDocumentSession documentSession, IQuerySession querySession, ProcessRegistry registry)
    {
        _documentSession = documentSession;
        _querySession = querySession;
        _registry = registry;
    }

    public Task<IReadOnlyList<IEvent>> FetchStreamAsync(Guid id, CancellationToken ct) =>
        _documentSession.Events.FetchStreamAsync(id, token: ct);

    public async Task<T?> AggregateStreamAsync<T>(Guid id, CancellationToken ct) where T : Aggregate =>
        await _querySession.Events.AggregateStreamAsync<T>(id, token: ct);

    public async Task<object?> AggregateStreamFromRegistryAsync(Type aggregateType,
        IEnumerable<object> events,
        Func<Task<IEnumerable<object>>> fetchEvents)
    {
        var eventList = events?.ToList() ?? (await fetchEvents()).ToList();
        var aggregate = CreateAggregate(aggregateType);

        foreach (var @event in eventList)
        {
            var applyMethod = _registry.GetApplyMethod(aggregateType, @event.GetType());
            applyMethod(aggregate, @event);
        }

        return aggregate;
    }


    private object CreateAggregate(Type aggregateType)
    {
        if (aggregateType.GetConstructor(Type.EmptyTypes) == null)
        {
            throw new InvalidOperationException($"Aggregate type {aggregateType.Name} must have a parameterless constructor.");
        }

        return Activator.CreateInstance(aggregateType)
               ?? throw new InvalidOperationException($"Failed to create instance of {aggregateType.Name}.");
    }


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