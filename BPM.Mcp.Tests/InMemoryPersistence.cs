using System.Collections.Concurrent;
using BPM.Core.Application.Persistence;
using BPM.Core.Configuration;
using BPM.Core.Events;
using BPM.Core.Persistence;
using BPM.Core.Process;
using JasperFx.Events;

namespace BPM.Mcp.Tests;

/// <summary>
/// Shared in-memory event streams backing both the engine-side
/// <see cref="IProcessStore"/> and the application-layer
/// <see cref="IProcessInstanceStore"/> — the integration tests run the full
/// stack without Postgres, which also proves the persistence seam holds.
/// </summary>
public sealed class InMemoryEventStore
{
    public sealed record StreamData(string AggregateName, List<object> Events, DateTimeOffset Started);

    private readonly ConcurrentDictionary<Guid, StreamData> _streams = new();

    public StreamData? Get(Guid id) => _streams.TryGetValue(id, out var s) ? s : null;

    public void Append(Guid id, string aggregateName, IEnumerable<object> events)
    {
        var stream = _streams.GetOrAdd(id, _ => new StreamData(aggregateName, [], DateTimeOffset.UtcNow));
        lock (stream.Events)
        {
            stream.Events.AddRange(events);
        }
    }
}

public sealed class InMemoryBpmRepository(InMemoryEventStore store, ProcessRegistry registry) : IBpmRepository
{
    public Task<IReadOnlyList<IEvent>> FetchStreamAsync(Guid id, CancellationToken ct) =>
        throw new NotSupportedException("Not used by the in-memory process store.");

    public Task<T?> AggregateStreamAsync<T>(Guid id, CancellationToken ct) where T : Aggregate =>
        throw new NotSupportedException("Not used by the in-memory process store.");

    public object AggregateStreamFromRegistry(Type aggregateType, IEnumerable<object> events)
    {
        var aggregate = FastActivator.CreateAggregate(aggregateType)!;
        var applied = 0;
        foreach (var @event in events)
        {
            var apply = registry.GetApplyMethodOrNull(aggregateType, @event.GetType());
            if (apply is null)
                continue;
            applied++;
            apply(aggregate, @event);
        }

        if (applied == 0)
            throw new InvalidOperationException($"No Apply methods found for aggregate {aggregateType.Name}.");
        return aggregate;
    }

    public object? AggregateOrNullStreamFromRegistry(Type aggregateType, IEnumerable<object> events)
    {
        var aggregate = FastActivator.CreateAggregate(aggregateType)!;
        foreach (var @event in events)
        {
            var apply = registry.GetApplyMethodOrNull(aggregateType, @event.GetType());
            if (apply is null)
                return null;
            apply(aggregate, @event);
        }

        return aggregate;
    }

    public object AggregateOrDefaultStreamFromRegistry(Type aggregateType, IEnumerable<object> events)
    {
        var aggregate = FastActivator.CreateAggregate(aggregateType)!;
        foreach (var @event in events)
        {
            registry.GetApplyMethodOrNull(aggregateType, @event.GetType())?.Invoke(aggregate, @event);
        }

        return aggregate;
    }

    public bool TryAggregateAs<T>(out T? aggregate, IEnumerable<object> events) where T : Aggregate
    {
        aggregate = null;
        try
        {
            aggregate = (T)AggregateStreamFromRegistry(typeof(T), events);
            return true;
        }
        catch
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
        catch
        {
            return false;
        }
    }

    public Task AppendEvents(Guid aggregateId, object[] events, bool newStream = true,
        Dictionary<string, object>? headers = null, CancellationToken ct = default)
    {
        if (events.Length > 0)
        {
            var aggregateName = headers?["AggregateType"].ToString()
                                ?? throw new InvalidOperationException("AggregateType header missing.");
            store.Append(aggregateId, aggregateName, events);
        }

        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default) => Task.CompletedTask;

    public Task<long> Update(Aggregate aggregate, long? expectedVersion = null, CancellationToken ct = default) =>
        throw new NotSupportedException("Not used by the in-memory process store.");
}

/// <summary>Mirrors <see cref="ProcessStore"/> against the in-memory streams.</summary>
public sealed class InMemoryProcessStore(InMemoryEventStore store, IBpmRepository repository) : IProcessStore
{
    private readonly Queue<IProcess> _processes = [];

    public IProcess? StartProcess<T>() where T : Aggregate =>
        new Process(Guid.NewGuid(), typeof(T).Name, true, null, null, null, null, repository);

    public IProcess? StartProcess<T>(BpmEvent @event) where T : Aggregate => StartProcess(typeof(T), @event);

    public IProcess? StartProcess(Type aggregateType, BpmEvent @event)
    {
        var config = BProcessGraphConfiguration.GetConfig(aggregateType.Name)!;
        if (!config.RootNode.ContainsEvent(@event))
            return null;

        @event.NodeId = config.RootNode.NodeLevel;
        var process = new Process(Guid.NewGuid(), aggregateType.Name, true, null, [@event], null,
            config.RootNode.NextSteps, repository);
        _processes.Enqueue(process);
        return process;
    }

    public Task<IProcess> FetchProcessAsync(Guid aggregateId, CancellationToken ct)
    {
        var stream = store.Get(aggregateId)
                     ?? throw new InvalidOperationException($"No stream for process {aggregateId}.");
        var process = new Process(aggregateId, stream.AggregateName, false, stream.Events.ToArray(), null,
            stream.Started, null, repository);
        _processes.Enqueue(process);
        return Task.FromResult<IProcess>(process);
    }

    public async Task SaveChangesAsync(CancellationToken token)
    {
        while (_processes.Count > 0)
            await ((Process)_processes.Dequeue()).AppendUncommittedToDb(token);
    }
}

public sealed class InMemoryProcessInstanceStore(InMemoryEventStore store) : IProcessInstanceStore
{
    public Task<ProcessInstanceSnapshot?> LoadAsync(Guid processId, CancellationToken ct)
    {
        var stream = store.Get(processId);
        if (stream is null || stream.Events.Count == 0)
            return Task.FromResult<ProcessInstanceSnapshot?>(null);

        var envelopes = stream.Events
            .Select((e, i) => new ProcessEventEnvelope(e.GetType().Name, e, i + 1, stream.Started))
            .ToList();
        return Task.FromResult<ProcessInstanceSnapshot?>(
            new ProcessInstanceSnapshot(processId, stream.AggregateName, envelopes, stream.Started));
    }
}
