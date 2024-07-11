using Core.BPM.Attributes;
using Credo.Core.Shared.Library;
using MediatR;

namespace MyCredo.Features.RecoveringPassword.IdentifyingFace;

[BpmProducer(typeof(IdentifiedFace))]
public record IdentifyFace(Guid DocumentId) : IRequest<Result>;

public record IdentifyFaceHandler() : IRequestHandler<IdentifyFace, Result>
{
    public Task<Result> Handle(IdentifyFace request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}