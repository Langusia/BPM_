using Core.BPM.Interfaces;
using Marten.Events;

namespace Core.BPM;

public class Aggregate : IAggregate
{
    public Guid Id { get; set; }

    public int Version { get; protected set; }


    public Dictionary<string, int> Counters = new();


    public void Apply(object @event)
    {
        var a = 1;
    }

    [NonSerialized] private readonly Queue<object> uncommittedEvents = new();

    public virtual void When(object @event)
    {
    }

    public object[] DequeueUncommittedEvents()
    {
        var dequeuedEvents = uncommittedEvents.ToArray();

        uncommittedEvents.Clear();

        return dequeuedEvents;
    }

    protected void Enqueue(object @event)
    {
        uncommittedEvents.Enqueue(@event);
    }
}