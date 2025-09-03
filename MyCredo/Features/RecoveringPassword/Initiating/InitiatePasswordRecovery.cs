using Core.BPM.Application.Managers;
using Core.BPM.Attributes;
using MediatR;
using MyCredo.Common;
using MyCredo.Features.TwoFactor;

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
        var process = store.StartProcess<PasswordRecovery>(new PasswordRecoveryInitiated(request.PersonalNumber, request.BirthDate, ChannelTypeEnum.MOBILE_CIB, false));
        var s = process!.GetNextSteps();
        process.AppendEvent(new Ad(process.Id));
        s = process!.GetNextSteps();
        process.AppendEvent(new Bd(process.Id));
        s = process!.GetNextSteps();
        process.AppendEvent(new Cd(process.Id, true));
        s = process!.GetNextSteps();
        if (!process.TryAggregateAs<PasswordRecovery>(out var a))
        {
            var ss = 1;
        }

        var r = process.AppendEvent(new Zd(process.Id));
        s = process!.GetNextSteps();

        await store.SaveChangesAsync(cancellationToken);


        return process.Id;
    }
}