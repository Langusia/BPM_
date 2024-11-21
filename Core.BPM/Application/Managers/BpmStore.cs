using Core.BPM.Configuration;
using Core.BPM.Persistence;
using Marten;
using Marten.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Core.BPM.Application.Managers;

public class BpmStore<TAggregate, TCommand>(IDocumentSession session, ILogger<BpmStore<TAggregate, TCommand>> logger, MartenRepository martenRepository)
    where TAggregate : Aggregate where TCommand : IBaseRequest
{
    private TAggregate? _aggregate;
    private ILogger<BpmStore<TAggregate, TCommand>> _logger = logger;
    private IReadOnlyList<IEvent> _persistedProcessEvents;
    private string _aggregateName;
    private BProcess? _config;
    private bool _newStream;

    public ProcessState<TAggregate> StartProcess(Action<TAggregate> action)
    {
        _aggregate = Activator.CreateInstance<TAggregate>();
        action(_aggregate);
        _aggregate.Id = Guid.NewGuid();
        _aggregateName = typeof(TAggregate).Name;
        _config = BProcessGraphConfiguration.GetConfig(_aggregateName!);
        _newStream = true;
        return new ProcessState<TAggregate>(_aggregate, _aggregate);
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            var expVersion = _aggregate.Version;
            var evts = _aggregate!.DequeueUncommittedEvents();
            if (evts is not null || evts!.Any())
            {
                session.SetHeader("AggregateType", _aggregateName);
                if (_newStream)
                    session.Events.StartStream<TAggregate>(_aggregate.Id, evts);
                else
                    await session.Events.AppendExclusive(_aggregate.Id, token: ct, evts);

                await session.SaveChangesAsync(ct);
                _newStream = false;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SAVE ERROR", _aggregate);
            throw;
        }
    }

    public async Task<ProcessState<TAggregate>> AggregateProcessStateAsync(Guid aggregateId, CancellationToken ct)
    {
        _persistedProcessEvents = await martenRepository.FetchStreamAsync(aggregateId, ct);
        var originalAggregateName = _persistedProcessEvents?.FirstOrDefault()?.Headers["AggregateType"].ToString();
        _aggregate = await martenRepository.AggregateStreamAsync<TAggregate>(aggregateId, ct);

        _config = BProcessGraphConfiguration.GetConfig(originalAggregateName!);
        _aggregate.PersistedEvents = _persistedProcessEvents!.Select(x => x.EventType.Name).ToList();
        if (_aggregate is null)
            return null;

        if (_config is null)
            return null;

        object originalAggregate = _aggregate;
        if (typeof(TAggregate) != _config.ProcessType)
        {
            originalAggregate = await martenRepository.AggregateStreamFromRegistryAsync(_config.ProcessType, _persistedProcessEvents.Select(x => x.Data), null);
        }

        return new ProcessState<TAggregate>(_aggregate!, originalAggregate, _config, typeof(TCommand));
    }
}