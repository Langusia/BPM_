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
    : IRequest<Guid>;

public class InitiatePasswordRecoveryHandler(IBpmStore store)
    : IRequestHandler<InitiatePasswordRecovery, Guid>
{
    public async Task<Guid> Handle(InitiatePasswordRecovery request, CancellationToken cancellationToken)
    {
        var process = store.StartProcess<PasswordRecovery>(new PasswordRecoveryInitiated(request.PersonalNumber, request.BirthDate, ChannelTypeEnum.MOBILE_CIB));
        process.AppendEvent(new TwoFactor.OtpSent(Guid.Empty, ""));
        process.AppendEvent(new OtpValidated(Guid.Empty, true));
        var nexts = process.GetNextSteps();

        //process!.AppendEvent(new Fd(Guid.Empty));

        var res = process.TryAggregateAs<PasswordRecovery>(out var agg);

        var a = 5;
        //List<object> strs =
        //[
        //    new OtpSent(Guid.Empty, ""),
        //    new OtpSent(Guid.Empty, ""),
        //    new OtpSent(Guid.Empty, ""),
        //    new OtpSent(Guid.Empty, ""),
        //    new OtpValidated(Guid.Empty, false),
        //    new OtpValidated(Guid.Empty, false),
        //    new OtpValidated(Guid.Empty, false)
        //];
        //Queue<object> strsQ = [];
        //var s = strs.Union(strsQ);
        //var pr = await store.FetchProcessAsync(Guid.Empty, cancellationToken);
        //process.AppendEvents(new Ad(Guid.Empty));
        //process.AppendEvents(new Bd(Guid.Empty));
        //process.AppendEvents(new Cd(Guid.Empty));
        //


        return process.Id;
    }
}