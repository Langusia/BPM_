using Credo.Core.Shared.Library;
using MediatR;

namespace MyCredo.Features.RecoveringPassword.ChallengingSecurityQuestion;

public record ValidateSecurityQuestion(Guid DocumentId) : IRequest<Result>;