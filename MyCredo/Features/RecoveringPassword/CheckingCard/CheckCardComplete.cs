using Core.BPM.MediatR.Attributes;
using MediatR;

namespace MyCredo.Features.RecoveringPassword.CheckingCard;

[BpmProducer(typeof(CheckCardCompleted))]
public record CheckCardComplete(Guid DocumentId) : IRequest<bool>;

public class CheckCardCompleteHandler() : IRequestHandler<CheckCardComplete, bool>
{
    public Task<bool> Handle(CheckCardComplete request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}