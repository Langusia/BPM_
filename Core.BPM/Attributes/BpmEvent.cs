namespace Core.BPM.Attributes;

public abstract record BpmEvent
{
    public int NodeId { get; internal set; }
}