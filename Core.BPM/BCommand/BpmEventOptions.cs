namespace Core.BPM.BCommand;

public record BpmEventOptions
{
    public int PermittedTryCount { get; set; }
    public string BpmEventName { get; set; }
}

public record BpmProcessEventOptions
{
    public string ProcessName { get; set; }
    public List<BpmEventOptions> BpmEventOptions { get; set; }
}