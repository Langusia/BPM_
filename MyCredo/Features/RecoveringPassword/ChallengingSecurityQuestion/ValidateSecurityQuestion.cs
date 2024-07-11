using Core.BPM.Attributes;
using Credo.Core.Shared.Library;
using MediatR;

namespace MyCredo.Features.RecoveringPassword.ChallengingSecurityQuestion;

[BpmProducer(typeof(SecurityQuestionValidated))]
public record ValidateSecurityQuestion(Guid DocumentId) : IRequest<Result>;

public class ValidateSecurityQuestionHandler() : IRequestHandler<ValidateSecurityQuestion, Result>
{
    public Task<Result> Handle(ValidateSecurityQuestion request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}