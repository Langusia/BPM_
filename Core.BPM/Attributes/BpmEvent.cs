namespace Core.BPM.Attributes;

public record BpmEvent
{
    public int? TryCount { get; set; }
}