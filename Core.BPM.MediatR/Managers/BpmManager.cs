using Core.BPM.Configuration;
using Core.BPM.MediatR.Attributes;
using Credo.Core.Shared.Library;
using Marten;

namespace Core.BPM.MediatR.Managers;

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
        var @events = await QSession.Events.FetchStreamAsync(aggregateId, token: ct);
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
            var concreteEventConfig = eventConfig.BpmEventOptions.FirstOrDefault(x => x.BpmEventName == concreteUpcoming.EventTypes.FirstOrDefault().Name);
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

    public async Task<Guid> StartProcess(T aggregate, CancellationToken token)
    {
        aggregate.Id = Guid.NewGuid();
        _session.SetHeader("AggregateType", aggregate.GetType().Name);
        _session.Events.StartStream<T>(aggregate.Id, aggregate.DequeueUncommittedEvents());
        await _session.SaveChangesAsync(token: token).ConfigureAwait(false);
        return aggregate.Id;
    }

    public async Task<T?> Get<T>(Guid aggregateId, CancellationToken token) where T : Aggregate
    {
        return await QSession.Events.AggregateStreamAsync<T>(aggregateId, token: token);
    }
}