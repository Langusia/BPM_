namespace Core.BPM.Events;

public abstract record BpmEvent
{
    public int NodeId { get; set; }
}