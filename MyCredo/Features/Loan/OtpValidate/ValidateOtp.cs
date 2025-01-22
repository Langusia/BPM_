using Core.BPM.Application.Managers;
using Core.BPM.Attributes;
using Credo.Core.Shared.Library;
using Credo.Core.Shared.Mediator;
using MyCredo.Common;

namespace MyCredo.Features.Loan.OtpValidate;

[BpmProducer(typeof(OtpValidated))]
public record ValidateOtp(Guid ProcessId, int UserId, ChannelTypeEnum Channel, string Otp) : ICommand<AggregateResult<bool>>;

internal class ValidateOtpCommandHandler(
) : ICommandHandler<ValidateOtp, AggregateResult<bool>>
{
    public async Task<Result<AggregateResult<bool>>> Handle(ValidateOtp request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}