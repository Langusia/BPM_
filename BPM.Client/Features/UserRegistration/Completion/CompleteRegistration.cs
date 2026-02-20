using Core.BPM.Application.Managers;
using Core.BPM.Attributes;
using MediatR;

namespace BPM.Client.Features.UserRegistration.Completion;

[BpmProducer(typeof(RegistrationCompleted))]
public record CompleteRegistration(Guid ProcessId) : IRequest;

public class CompleteRegistrationHandler(IBpmStore store) : IRequestHandler<CompleteRegistration>
{
    public async Task Handle(CompleteRegistration request, CancellationToken cancellationToken)
    {
        var process = await store.FetchProcessAsync(request.ProcessId, cancellationToken);
        process.AppendEvent(new RegistrationCompleted());
        await store.SaveChangesAsync(cancellationToken);
    }
}
