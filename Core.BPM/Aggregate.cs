using Core.BPM.Attributes;
using Core.BPM.Interfaces;

namespace Core.BPM;

public class Aggregate : IAggregate
{
    public Guid Id { get; set; }
    public int Version { get; protected set; }
    public Dictionary<string, int> EventCounters = new();
    public List<string> PersistedEvents { get; set; } = new();

    [NonSerialized] public readonly Queue<object> UncommittedEvents = new();
    public object? LastUncommitedEvent;

    public virtual void When(object @event)
    {
    }

    public object[] DequeueUncommittedEvents()
    {
        var dequeuedEvents = UncommittedEvents.ToArray();
        PersistedEvents.AddRange(dequeuedEvents.Select(x => x.GetType().Name));
        UncommittedEvents.Clear();
        return dequeuedEvents;
    }

    public void Enqueue(object @event)
    {
        UncommittedEvents.Enqueue(@event);
        LastUncommitedEvent = @event;
    }
}