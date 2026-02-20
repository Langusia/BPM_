using Core.BPM.Attributes;

namespace BPM.Client.Features.UserRegistration;

public record RegistrationInitiated(string Email, string FullName) : BpmEvent;

public record EmailVerified(bool IsValid) : BpmEvent;

public record ProfileSetUp(string DisplayName, string? AvatarUrl) : BpmEvent;

public record RegistrationCompleted() : BpmEvent;
