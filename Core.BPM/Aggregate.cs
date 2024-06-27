using Core.BPM.BCommand;
using Core.BPM.Interfaces;

namespace Core.BPM;

public class Aggregate : IAggregate
{
    public Guid Id { get; set; }
    public int Version { get; protected set; }
    public Dictionary<string, int> EventCounters = new();
    public List<string> PersistedEvents { get; set; } = new();

    [NonSerialized] protected readonly Queue<object> UncommittedEvents = new();

    public virtual void When(object @event)
    {
    }

    public object[] DequeueUncommittedEvents()
    {
        var dequeuedEvents = UncommittedEvents.ToArray();

        UncommittedEvents.Clear();

        return dequeuedEvents;
    }

    public void SetBpmProps(BpmEvent @event)
    {
        if (@event.TryCount is not null)
            if (!EventCounters.ContainsKey(@event.GetType().Name))
                EventCounters.Add(@event.GetType().Name, 1);
            else
                EventCounters[@event.GetType().Name] += 1;

        PersistedEvents.Add(@event.GetType().Name);
    }

    protected void Enqueue(object @event)
    {
        UncommittedEvents.Enqueue(@event);
    }
}