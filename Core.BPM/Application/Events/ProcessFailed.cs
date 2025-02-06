namespace Core.BPM.Application.Events;

public record ProcessFailed(string FailOrigin, object Data, string Description);