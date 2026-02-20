using BPM.Core.Process;
using BPM.Core.Definition;
using BPM.Core.Definition.Interfaces;
using BPM.Client.Features.UserRegistration.Initiating;
using BPM.Client.Features.UserRegistration.VerifyEmail;
using BPM.Client.Features.UserRegistration.SetupProfile;
using BPM.Client.Features.UserRegistration.Completion;

namespace BPM.Client.Features.UserRegistration;

public class UserRegistration : Aggregate
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; }
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsComplete { get; set; }

    public void Apply(RegistrationInitiated @event)
    {
        Email = @event.Email;
        FullName = @event.FullName;
    }

    public void Apply(EmailVerified @event)
    {
        IsEmailVerified = @event.IsValid;
    }

    public void Apply(ProfileSetUp @event)
    {
        DisplayName = @event.DisplayName;
        AvatarUrl = @event.AvatarUrl;
    }

    public void Apply(RegistrationCompleted @event)
    {
        IsComplete = true;
    }
}

public class UserRegistrationDefinition : BpmDefinition<UserRegistration>
{
    public override ProcessConfig<UserRegistration> DefineProcess(IProcessBuilder<UserRegistration> configureProcess)
    {
        return configureProcess
            .StartWith<InitiateRegistration>()
            .Continue<VerifyUserEmail>()
            .Continue<SetupUserProfile>()
            .Continue<CompleteRegistration>()
            .End();
    }
}
