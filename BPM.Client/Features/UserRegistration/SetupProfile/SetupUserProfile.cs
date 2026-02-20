using BPM.Core.Process;
using BPM.Core.Attributes;
using MediatR;

namespace BPM.Client.Features.UserRegistration.SetupProfile;

[BpmProducer(typeof(ProfileSetUp))]
public record SetupUserProfile(Guid ProcessId, string DisplayName, string? AvatarUrl) : IRequest;

public class SetupUserProfileHandler(IProcessStore store) : IRequestHandler<SetupUserProfile>
{
    public async Task Handle(SetupUserProfile request, CancellationToken cancellationToken)
    {
        var process = await store.FetchProcessAsync(request.ProcessId, cancellationToken);
        process.AppendEvent(new ProfileSetUp(request.DisplayName, request.AvatarUrl));
        await store.SaveChangesAsync(cancellationToken);
    }
}
