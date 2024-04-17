using Core.BPM.MediatR.Attributes;
using MediatR;

namespace MyCredo.Features.RecoveringPassword.CheckingCard;

[BpmProducer(typeof(CheckCardInitiated))]
public record CheckCardInitiate(Guid DocumentId) : IRequest<bool>;

public record CheckCardInitiateHandler() : IRequestHandler<CheckCardInitiate, bool>
{
    public Task<bool> Handle(CheckCardInitiate request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}