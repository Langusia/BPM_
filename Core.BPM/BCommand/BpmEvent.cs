namespace Core.BPM.BCommand;

public record BpmEvent
{
    public int? TryCount { get; set; }
}