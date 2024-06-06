using Core.BPM.Application.Managers;
using Core.BPM.MediatR.Attributes;
using Credo.Core.Shared.Library;
using Credo.Core.Shared.Mediator;
using MyCredo.Common;
using MyCredo.Retail.Loan.Application.Features.RequestLoanProcess.CarPawnshop;

namespace MyCredo.Retail.Loan.Application.Features.TwoFactor.OtpSend;

[BpmProducer(typeof(OtpSent))]
public record SendOtp(Guid ProcessId, int UserId, ChannelTypeEnum Channel) : ICommand<AggregateResult<bool>>;

public class SendOtpCommandHandler(
    BpmManager<OtpValidation> _bpmManager
) : ICommandHandler<SendOtp, AggregateResult<bool>>
{
    public async Task<Result<AggregateResult<bool>>> Handle(SendOtp request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}