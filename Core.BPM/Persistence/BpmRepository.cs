using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Core.BPM.Registry;
using Marten;
using Marten.Events;

namespace Core.BPM.Persistence;

public class BpmRepository : IBpmRepository
{
    private readonly IDocumentSession _documentSession;
    private readonly IQuerySession _querySession;
    private readonly ProcessRegistry _registry;

    public BpmRepository(IDocumentSession documentSession, IQuerySession querySession, ProcessRegistry registry)
    {
        _documentSession = documentSession;
        _querySession = querySession;
        _registry = registry;
    }

    public async Task<IReadOnlyList<IEvent>> FetchStreamAsync(Guid id, CancellationToken ct) =>
        await _documentSession.Events.FetchStreamAsync(id, token: ct);

    public async Task<T?> AggregateStreamAsync<T>(Guid id, CancellationToken ct) where T : Aggregate =>
        await _querySession.Events.AggregateStreamAsync<T>(id, token: ct);

    public object AggregateStreamFromRegistry(Type aggregateType, IEnumerable<object> events)
    {
        var eventList = events.ToList();
        var aggregate = CreateAggregate(aggregateType);
        int count = 0;
        foreach (var @event in eventList)
        {
            var applyMethod = _registry.GetApplyMethod(aggregateType, @event.GetType());
            if (applyMethod is not null)
            {
                count++;
                applyMethod(aggregate, @event);
            }
        }

        if (count == 0)
            throw new InvalidOperationException($"No Apply methods found for aggregate {aggregateType.Name}.");

        return aggregate;
    }

    public object? AggregateOrNullStreamFromRegistry(Type aggregateType, IEnumerable<object> events)
    {
        var eventList = events.ToList();
        var aggregate = CreateAggregate(aggregateType);

        foreach (var @event in eventList)
        {
            var applyMethod = _registry.GetApplyMethodOrNull(aggregateType, @event.GetType());
            if (applyMethod is null)
                return null;
            applyMethod(aggregate, @event);
        }

        return aggregate;
    }

    public object AggregateOrDefaultStreamFromRegistry(Type aggregateType, IEnumerable<object> events)
    {
        var eventList = events.ToList();
        var aggregate = CreateAggregate(aggregateType);

        foreach (var @event in eventList)
        {
            var applyMethod = _registry.GetApplyMethodOrNull(aggregateType, @event.GetType());
            if (applyMethod is null)
                continue;
            applyMethod(aggregate, @event);
        }

        return aggregate;
    }

    public bool TryAggregateAs<T>(out T? aggregate, IEnumerable<object> stream) where T : Aggregate
    {
        aggregate = null;
        try
        {
            aggregate = (T)AggregateStreamFromRegistry(typeof(T), stream);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public bool TryAggregateAs(Type aggType, IEnumerable<object> stream, out Aggregate? aggregate)
    {
        aggregate = null;
        try
        {
            aggregate = (Aggregate)AggregateStreamFromRegistry(aggType, stream);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }


    public async Task AppendEvents(Guid aggregateId, object[] events, bool newStream = true, Dictionary<string, object>? headers = null, CancellationToken ct = default)
    {
        if (events.Any())
        {
            foreach (var header in headers)
            {
                _documentSession.SetHeader(header.Key, header.Value);
            }

            if (newStream)
                _documentSession.Events.StartStream(aggregateId, events);
            else
                await _documentSession.Events.AppendExclusive(aggregateId, ct, events);
        }
    }

    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        await _documentSession.SaveChangesAsync(ct);
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

    private object CreateAggregate(Type aggregateType)
    {
        return FastActivator.CreateAggregate(aggregateType);
    }
}

public interface IBpmRepository : IEventStoreRepository, IProcessRegistryRepository;