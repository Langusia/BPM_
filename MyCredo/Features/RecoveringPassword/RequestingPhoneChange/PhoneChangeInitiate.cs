using Core.BPM.Attributes;
using MediatR;

namespace MyCredo.Features.RecoveringPassword.RequestingPhoneChange;

[BpmProducer(typeof(PhoneChangeInitiated))]
public record PhoneChangeInitiate(Guid DocumentId) : IRequest<bool>;

public class PhoneChangeInitiateHandler() : IRequestHandler<PhoneChangeInitiate, bool>
{
    public Task<bool> Handle(PhoneChangeInitiate request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}