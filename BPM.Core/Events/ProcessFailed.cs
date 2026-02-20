namespace BPM.Core.Events;

public record ProcessFailed(string FailOrigin, object Data, string Description);