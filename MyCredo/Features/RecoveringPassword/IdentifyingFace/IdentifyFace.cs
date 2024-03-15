using Credo.Core.Shared.Library;
using MediatR;

namespace MyCredo.Features.RecoveringPassword.IdentifyingFace;

public record IdentifyFace(Guid DocumentId) : IRequest<Result>;