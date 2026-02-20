namespace Core.BPM.Events;

public record ProcessFailed(string FailOrigin, object Data, string Description);