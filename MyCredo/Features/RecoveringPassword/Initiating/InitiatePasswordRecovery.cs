using Core.BPM.Application.Managers;
using Core.BPM.Attributes;
using Marten;
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
        var process = store.StartProcess<PasswordRecovery>(new PasswordRecoveryInitiated(request.PersonalNumber, request.BirthDate, request.ChannelType));
       //process.AppendEvents(new Ad(Guid.Empty));
       //process.AppendEvents(new Cd(Guid.Empty));
       //process.AppendEvents(new Zd(Guid.Empty));
       //
        var s = process.GetNextSteps();

        return Guid.Empty;
    }
}