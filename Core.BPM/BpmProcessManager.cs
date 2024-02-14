using Core.BPM.Interfaces;
using Marten;

namespace Core.BPM;

public interface IEventOutput
{
    string[] NextEvents { get; set; }
}

public class BpmProcessManager<TProcess, TEvent>
    where TProcess : class, IProcess
    where TEvent : IEvent
    //where TOutput : IEventOutput
{
    private readonly IDocumentSession _session;
    private readonly IQuerySession _querySession;

    public BpmProcessManager(IDocumentSession session, IQuerySession querySession)
    {
        _session = session;
        _querySession = querySession;
    }

    public async Task Handle(TEvent request)
    {
        var cfg = BpmProcessGraphConfiguration.GetConfig<TProcess>();
        if (cfg is null)
            return;

        var aggregateSnapshot = await _session.Events.AggregateStreamAsync<TProcess>(request.DocumentId);
        var events = await _querySession.Events.FetchStreamAsync(request.DocumentId);
        var lastEvent = events.OrderDescending().FirstOrDefault();
        if (lastEvent == null)
            return;

        var nextEvents = cfg.RootNode.TraverseTo(lastEvent.GetType())?.NextSteps;
        if (nextEvents is null)
            return;

        if (nextEvents.All(x => x.EventType != typeof(TEvent)))
            return;
    }
}