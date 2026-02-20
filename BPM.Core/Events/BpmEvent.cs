namespace BPM.Core.Events;

public abstract record BpmEvent
{
    public int NodeId { get; set; }
}