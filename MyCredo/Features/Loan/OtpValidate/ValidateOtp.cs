using Core.BPM.Application.Managers;
using Core.BPM.MediatR.Attributes;
using Credo.Core.Shared.Library;
using Credo.Core.Shared.Mediator;
using MyCredo.Common;
using MyCredo.Retail.Loan.Application.Features.RequestLoanProcess.CarPawnshop;

namespace MyCredo.Retail.Loan.Application.Features.TwoFactor.OtpValidate;

[BpmProducer(typeof(OtpValidated))]
public record ValidateOtp(Guid ProcessId, int UserId, ChannelTypeEnum Channel, string Otp) : ICommand<AggregateResult<bool>>;

internal class ValidateOtpCommandHandler(
    BpmManager<OtpValidation> _bpmManager
) : ICommandHandler<ValidateOtp, AggregateResult<bool>>
{
    public async Task<Result<AggregateResult<bool>>> Handle(ValidateOtp request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}