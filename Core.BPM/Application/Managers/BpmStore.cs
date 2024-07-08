using Core.BPM.Configuration;
using Marten;
using Marten.Events;
using MediatR;

namespace Core.BPM.Application.Managers;

public class BpmStore<TAggregate, TCommand>(IDocumentSession session) where TAggregate : Aggregate where TCommand : IBaseRequest
{
    private readonly IQuerySession _qSession = session;

    private TAggregate? _aggregate;
    private IReadOnlyList<IEvent> _persistedProcessEvents;
    private string _aggregateName;
    private BProcess? _config;
    private bool _newStream;

    public async Task<ProcessState<TAggregate>> StartProcess(Action<TAggregate> action, CancellationToken token)
    {
        action(_aggregate);
        _aggregate.Id = Guid.NewGuid();
        _aggregateName = typeof(TAggregate).Name;
        _config = BProcessGraphConfiguration.GetConfig(_aggregateName!);
        return new ProcessState<TAggregate>(_aggregate);
    }

    public ProcessState<TAggregate> StartProcess(TAggregate aggregate)
    {
        aggregate.Id = Guid.NewGuid();
        _aggregate = aggregate;
        _aggregateName = aggregate.GetType().Name;
        _config = BProcessGraphConfiguration.GetConfig(_aggregateName!);
        _newStream = true;
        return new ProcessState<TAggregate>(_aggregate);
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        session.SetHeader("AggregateType", _aggregateName);
        if (_newStream)
            session.Events.StartStream<TAggregate>(_aggregate.Id, _aggregate.DequeueUncommittedEvents());
        else
            session.Events.Append(_aggregate.Id, _aggregate.DequeueUncommittedEvents());

        await session.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<ProcessState<TAggregate>> AggregateProcessStateAsync(Guid aggregateId, CancellationToken ct)
    {
        _persistedProcessEvents = await _qSession.Events.FetchStreamAsync(aggregateId, token: ct);
        var originalAggregateName = _persistedProcessEvents?.FirstOrDefault()?.Headers["AggregateType"].ToString();
        _aggregate = await _qSession.Events.AggregateStreamAsync<TAggregate>(aggregateId, token: ct);
        _config = BProcessGraphConfiguration.GetConfig(originalAggregateName!);

        if (_aggregate is null)
            return null;

        if (_config is null)
            return null;

        return new ProcessState<TAggregate>(_aggregate!, _config, _persistedProcessEvents!.Select(x => x.EventType.Name).ToList(), typeof(TCommand));
    }
}