using Core.BPM;
using Core.BPM.Interfaces;
using Core.Persistence.Exceptions;
using IEvent = Marten.Events.IEvent;

namespace Core.Persistence;

public static class RepositoryExtensions
{
    public static async Task<T> Get<T>(
        this MartenRepository<T> repository,
        Guid id,
        CancellationToken cancellationToken = default
    ) where T : Aggregate
    {
        var entity = await repository.Find(id, cancellationToken).ConfigureAwait(false);

        return entity ?? throw AggregateNotFoundException.For<T>(id);
    }

    public static async Task<Tuple<IReadOnlyList<IEvent>, T>> GetWithEvents<T>(
        this MartenRepository<T> repository,
        Guid id,
        CancellationToken cancellationToken = default
    ) where T : Aggregate
    {
        var entity = await repository.Find(id, cancellationToken).ConfigureAwait(false);
        if (entity is null)
            throw AggregateNotFoundException.For<T>(id);

        var @events = await repository.FetchStreamAsync(id, ct: cancellationToken).ConfigureAwait(false);

        return new Tuple<IReadOnlyList<IEvent>, T>(@events, entity);
    }

    public static async Task<long> GetAndUpdate<T>(
        this MartenRepository<T> repository,
        Guid id,
        Action<T> action,
        long? expectedVersion = null,
        CancellationToken ct = default
    ) where T : Aggregate
    {
        var entity = await repository.Get(id, ct).ConfigureAwait(false);

        action(entity);

        return await repository.Update(entity, expectedVersion, ct).ConfigureAwait(false);
    }
}