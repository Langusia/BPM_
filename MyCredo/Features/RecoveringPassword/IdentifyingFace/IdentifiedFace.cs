using Core.BPM.MediatR;
using Core.BPM.MediatR.Mediator;

namespace MyCredo.Features.RecoveringPassword.IdentifyingFace;

public record IdentifiedFace(Guid DocumentId) : CredoEvent<IdentifyFace>;