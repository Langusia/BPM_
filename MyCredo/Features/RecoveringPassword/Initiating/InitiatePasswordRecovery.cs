using Core.BPM.Application.Managers;
using Core.BPM.Attributes;
using Marten;
using MediatR;
using MyCredo.Common;
using MyCredo.Features.Loan.OtpValidate;
using MyCredo.Features.TwoFactor;
using OtpSent = MyCredo.Features.Loan.OtpSend.OtpSent;

namespace MyCredo.Features.RecoveringPassword.Initiating;

[BpmProducer(typeof(PasswordRecoveryInitiated))]
public record InitiatePasswordRecovery(
    string PersonalNumber,
    DateTime BirthDate,
    ChannelTypeEnum ChannelType)
    : IRequest<List<string>>;

public class InitiatePasswordRecoveryHandler(IBpmStore store)
    : IRequestHandler<InitiatePasswordRecovery, List<string>>
{
    public async Task<List<string>> Handle(InitiatePasswordRecovery request, CancellationToken cancellationToken)
    {
        var process = store.StartProcess<PasswordRecovery>(new PasswordRecoveryInitiated(request.PersonalNumber, request.BirthDate, ChannelTypeEnum.MOBILE_CIB));
        var ress = process.AppendEvent(new Zd(process.Id));
        var nexts = process.GetNextSteps();

        //process!.AppendEvent(new Fd(Guid.Empty));

        var res = process.TryAggregateAs<PasswordRecovery>(out var agg);

        var a = 5;
        return nexts.Data.Select(x => x.CommandType.Name).ToList();
    }
}