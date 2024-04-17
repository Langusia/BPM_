using Core.BPM.MediatR.Attributes;
using MediatR;

namespace MyCredo.Features.RecoveringPassword.Finishing;

[BpmProducer(typeof(FinishedPasswordRecovery))]
public record FinishPasswordRecovery(Guid DocumentId) : IRequest<bool>;

public class FinishPasswordRecoveryHandler() : IRequestHandler<FinishPasswordRecovery, bool>
{
    public Task<bool> Handle(FinishPasswordRecovery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}