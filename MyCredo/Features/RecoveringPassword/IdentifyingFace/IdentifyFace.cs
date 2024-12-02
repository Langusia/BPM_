using Core.BPM.Attributes;
using Credo.Core.Shared.Library;
using Credo.Core.Shared.Mediator;
using MediatR;

namespace MyCredo.Features.RecoveringPassword.IdentifyingFace;

[BpmProducer(typeof(IdentifiedFace))]
public record IdentifyFace(Guid DocumentId) : ICommand;

public record IdentifyFaceHandler() : IRequestHandler<IdentifyFace, Result>
{
    public Task<Result> Handle(IdentifyFace request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}