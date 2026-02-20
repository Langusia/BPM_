using Core.BPM.Application.Managers;
using Core.BPM.Attributes;
using MediatR;

namespace BPM.Client.Features.UserRegistration.VerifyEmail;

[BpmProducer(typeof(EmailVerified))]
public record VerifyUserEmail(Guid ProcessId) : IRequest;

public class VerifyUserEmailHandler(IBpmStore store) : IRequestHandler<VerifyUserEmail>
{
    public async Task Handle(VerifyUserEmail request, CancellationToken cancellationToken)
    {
        var process = await store.FetchProcessAsync(request.ProcessId, cancellationToken);
        process.AppendEvent(new EmailVerified(true));
        await store.SaveChangesAsync(cancellationToken);
    }
}
