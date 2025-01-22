using Core.BPM.Application.Managers;
using Core.BPM.Attributes;
using Credo.Core.Shared.Library;
using Credo.Core.Shared.Mediator;
using MyCredo.Common;

namespace MyCredo.Features.Loan.OtpSend;

[BpmProducer(typeof(OtpSent))]
public record SendOtp(Guid ProcessId, int UserId, ChannelTypeEnum Channel) : ICommand<AggregateResult<bool>>;

public class SendOtpCommandHandler() : ICommandHandler<SendOtp, AggregateResult<bool>>
{
    public async Task<Result<AggregateResult<bool>>> Handle(SendOtp request, CancellationToken cancellationToken)
    {
        //if (state.ValidateFor<SendOtp>())
        //    return null;

        //BL

        //state.AppendEvent(x => x.SendOtp());
        return null;
    }
}