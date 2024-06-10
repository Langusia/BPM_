﻿using Core.BPM.Configuration;
using Core.BPM.MediatR.Attributes;
using Credo.Core.Shared.Library;
using Marten;

namespace Core.BPM.Application.Managers;

public class BpmManager
{
    private readonly IDocumentSession _session;
    protected readonly IQuerySession QSession;

    public BpmManager(IDocumentSession session, IQuerySession qSession)
    {
        _session = session;
        QSession = qSession;
    }


    public async Task AppendAsync(Guid aggregateId, object[] events, CancellationToken token)
    {
        _session.Events.Append(aggregateId, events);
        await _session.SaveChangesAsync(token).ConfigureAwait(false);
    }


    public async Task<Result> ValidateAsync<TCommand>(Guid aggregateId, CancellationToken ct)
    {
        var @events = await _session.Events.FetchStreamAsync(aggregateId, token: ct);
        var s = @events.First().AggregateTypeName;
        var aggregateName = @events.First().Headers!["AggregateType"];
        if (@events is null)
            return Result.Failure(new Error("process_not_found", "Process not found", ErrorTypeEnum.NotFound));

        var config = BProcessGraphConfiguration.GetConfig(aggregateName!.ToString());
        if (config is null)
            return Result.Failure(new Error("process_not_configured", "Process not configured", ErrorTypeEnum.BadRequest));


        var eventConfig = BProcessGraphConfiguration.GetEventConfig(aggregateName!.ToString());
        if (eventConfig is not null)
        {
            var concreteUpcoming = typeof(TCommand).GetCustomAttributes(typeof(BpmProducer), false).FirstOrDefault() as BpmProducer;
            var concreteEventConfig = eventConfig.BpmCommandtOptions.FirstOrDefault(x => x.BpmEventName == concreteUpcoming.EventTypes.FirstOrDefault().Name);
            if (concreteEventConfig is not null)
            {
                if (concreteEventConfig.PermittedTryCount == @events.Count(x => x.EventType.Name == concreteUpcoming.EventTypes.FirstOrDefault().Name))
                    return Result.Failure(new Error("process_event_tryCount_reached", "event is exceeding maximum try count", ErrorTypeEnum.NotFound));
            }
        }

        var currentCommandType = typeof(TCommand);
        var paths = config.MoveTo(currentCommandType);

        if (paths.All(x => x.PrevSteps != null &&
                           x.PrevSteps.All(z =>
                               ((BpmProducer)z.CommandType.GetCustomAttributes(typeof(BpmProducer), false).FirstOrDefault()!)
                               .EventTypes.Any(x => x != @events.Last().EventType))))
            return Result.Failure(new Error("process_event_wrong_path", "process path not waiting given event", ErrorTypeEnum.NotFound));


        return Result.Success();
    }
}

public class BpmManager<T>(IDocumentSession session, IQuerySession qSession) : BpmManager(session, qSession)
    where T : Aggregate
{
    private readonly IDocumentSession _session = session;
    private readonly IQuerySession _qSession = session;

    public async Task<BpmResult> StartProcess(T aggregate, CancellationToken token)
    {
        aggregate.Id = Guid.NewGuid();
        _session.SetHeader("AggregateType", aggregate.GetType().Name);
        _session.Events.StartStream<T>(aggregate.Id, aggregate.DequeueUncommittedEvents());
        await _session.SaveChangesAsync(token: token).ConfigureAwait(false);
        var rootNode = BProcessGraphConfiguration.GetConfig(aggregate.GetType().Name)!.RootNode;
        return new BpmResult { AggregateId = aggregate.Id, CurrentNodeStored = rootNode, NextNodes = rootNode.NextSteps?.Select(x => x.CommandType.Name).ToList() };
    }

    public async Task<T?> Get<T>(Guid aggregateId, CancellationToken token) where T : Aggregate
    {
        return await QSession.Events.AggregateStreamAsync<T>(aggregateId, token: token);
    }


    public async Task<Result<BpmResult<T>>> AggregateAsync<TCommand>(Guid aggregateId, CancellationToken ct)
    {
        var aggregateRawEvents = await _qSession.Events.FetchStreamAsync(aggregateId);
        var persistedEvents = aggregateRawEvents.Select(x => x.EventType.Name).ToList();
        var originalAggregateName = aggregateRawEvents.FirstOrDefault().Headers["AggregateType"].ToString();
        var aggregate = await _qSession.Events.AggregateStreamAsync<T>(aggregateId, token: ct);

        if (aggregate is null)
            return Result.Failure<BpmResult<T>>(new Error("process_not_configured", "Process not configured", ErrorTypeEnum.BadRequest));

        var config = BProcessGraphConfiguration.GetConfig(originalAggregateName!);
        if (config is null)
            return Result.Failure<BpmResult<T>>(new Error("process_not_configured", "Process not configured", ErrorTypeEnum.BadRequest));
        if (!config.CheckPathValid<TCommand>(aggregateRawEvents.Select(x => x.EventType.Name).ToList()))
            return Result.Failure<BpmResult<T>>(new Error("process_event_wrong_path", "process path not waiting given event", ErrorTypeEnum.NotFound));

        var eventConfig = BProcessGraphConfiguration.GetEventConfig<T>();
        if (eventConfig is not null && eventConfig.CheckTryCount<TCommand>(aggregateRawEvents.Select(x => x.EventTypeName).ToList()))
            return Result.Failure<BpmResult<T>>(new Error("process_event_tryCount_reached", "event is exceeding maximum try count", ErrorTypeEnum.NotFound));

        var currentNode = config.MoveTo(persistedEvents);
        var afterAppend = currentNode?.NextSteps?.FirstOrDefault(x => x.CommandType == typeof(TCommand));
        return Result.Success(new BpmResult<T>(aggregate)
        {
            AggregateId = aggregateId, CurrentNodeStored = currentNode, CurrentNodeAfterAppend = afterAppend,
            NextNodesAfterAppend = afterAppend?.NextSteps?.Select(x => x.CommandType.Name).ToList() ?? currentNode?.NextSteps?.Select(x => x.CommandType.Name).ToList(),
            NextNodes = currentNode?.NextSteps?.Select(x => x.CommandType.Name).ToList(),
        });
    }


    private static BpmProducer GetCommandProducer<TCommand>()
    {
        return (BpmProducer)typeof(TCommand).GetCustomAttributes(typeof(BpmProducer), false).FirstOrDefault()!;
    }

    private static BpmProducer GetCommandProducer(Type commandType)
    {
        return (BpmProducer)commandType.GetCustomAttributes(typeof(BpmProducer), false).FirstOrDefault()!;
    }
}