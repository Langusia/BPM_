﻿using Core.BPM.Attributes;
using Core.BPM.Interfaces;

namespace Core.BPM;

public abstract class Aggregate : IAggregate
{
    public Guid Id { get; set; }
    public int Version { get; protected set; }

    [NonSerialized] public readonly Queue<object> UncommittedEvents = new();

    //COMPLETITION SUMMARY
    public virtual bool? IsCompleted() => null;

    public object[] DequeueUncommittedEvents()
    {
        var dequeuedEvents = UncommittedEvents.ToArray();
        UncommittedEvents.Clear();
        return dequeuedEvents;
    }

    public void Enqueue(object @event)
    {
        UncommittedEvents.Enqueue(@event);
    }
}