using Core.BPM.Attributes;
using MediatR;

namespace MyCredo.Features.RecoveringPassword.RequestingPhoneChange;

[BpmProducer(typeof(PhoneChangeCompleted))]
public record PhoneChangeComplete(Guid DocumentId) : IRequest<bool>;

public class PhoneChangeCompleteHandler() : IRequestHandler<PhoneChangeComplete, bool>
{
    public Task<bool> Handle(PhoneChangeComplete request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}