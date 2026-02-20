using Core.BPM.Application.Managers;
using Core.BPM.Attributes;
using MediatR;

namespace BPM.Client.Features.UserRegistration.Initiating;

[BpmProducer(typeof(RegistrationInitiated))]
public record InitiateRegistration(string Email, string FullName) : IRequest<Guid>;

public class InitiateRegistrationHandler(IBpmStore store) : IRequestHandler<InitiateRegistration, Guid>
{
    public async Task<Guid> Handle(InitiateRegistration request, CancellationToken cancellationToken)
    {
        var process = store.StartProcess<UserRegistration>(
            new RegistrationInitiated(request.Email, request.FullName));
        await store.SaveChangesAsync(cancellationToken);
        return process.Id;
    }
}
