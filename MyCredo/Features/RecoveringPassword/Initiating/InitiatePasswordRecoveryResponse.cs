namespace MyCredo.Features.RecoveringPassword.Initiating;

public record InitiatePasswordRecoveryResponse(Guid ProcessId, string Mobile, bool IsMobileNumberChangeAllowed);