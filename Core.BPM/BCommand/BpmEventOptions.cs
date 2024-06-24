namespace Core.BPM.BCommand;

public record BpmEventOptions
{
    public bool Optional { get; set; }
    public bool AnyTime { get; set; }
    public int? PermittedTryCount { get; set; }
    public string BpmCommandName { get; set; }
    public string BpmEventName { get; set; }
}

public record BpmProcessEventOptions
{
    public string ProcessName { get; set; }
    public List<BpmEventOptions> BpmCommandtOptions { get; set; }
}