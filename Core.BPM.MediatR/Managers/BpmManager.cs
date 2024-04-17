using Core.BPM.Configuration;
using Core.BPM.MediatR.Attributes;
using Marten;

namespace Core.BPM.MediatR.Managers;

public class BpmManager
{
    private readonly IDocumentSession _session;
    private readonly IQuerySession _qSession;

    public BpmManager(IDocumentSession session, IQuerySession qSession)
    {
        _session = session;
        _qSession = qSession;
    }

    public async Task<T?> AggregateAsync<T>(Guid aggregateId, CancellationToken token) where T : Aggregate
    {
        return await _qSession.Events.AggregateStreamAsync<T>(aggregateId, token: token);
    }

    public async Task Append(Guid aggregateId, object[] events, CancellationToken token)
    {
        _session.Events.Append(aggregateId, events);
        await _session.SaveChangesAsync(token).ConfigureAwait(false);
    }

    public async Task<bool> ValidateAsync<TCommand>(Guid aggregateId, CancellationToken ct)
    {
        var @events = await _session.Events.FetchStreamAsync(aggregateId, token: ct);
        var aggregateName = @events.First().Headers!["AggregateType"];
        if (@events is null)
            return false;

        var config = BProcessGraphConfiguration.GetConfig(aggregateName!.ToString());
        if (config is null)
            return false;

        var currentCommandType = typeof(TCommand);
        var paths = config.MoveTo(currentCommandType);

        if (paths.All(x => x.PrevSteps != null &&
                           x.PrevSteps.All(z =>
                               ((BpmProducer)z.CommandType.GetCustomAttributes(typeof(BpmProducer), false).FirstOrDefault()!)
                               .EventTypes.Any(x => x != @events.Last().EventType))))
            return false;

        return true;
    }
}