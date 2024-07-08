using Core.BPM.Application.Managers;
using Core.BPM.MediatR.Attributes;
using Core.BPM.MediatR.Managers;
using Marten;
using MediatR;
using MyCredo.Common;

namespace MyCredo.Features.RecoveringPassword.Initiating;

[BpmProducer(typeof(PasswordRecoveryInitiated))]
public record InitiatePasswordRecovery(
    string PersonalNumber,
    DateTime BirthDate,
    ChannelTypeEnum ChannelType)
    : IRequest<Guid>;

public class InitiatePasswordRecoveryHandler(BpmStore<PasswordRecovery, InitiatePasswordRecovery> mgr)
    : IRequestHandler<InitiatePasswordRecovery, Guid>
{
    public async Task<Guid> Handle(InitiatePasswordRecovery request, CancellationToken cancellationToken)
    {
        var agg = mgr.StartProcess(new PasswordRecovery().Initiate(request.PersonalNumber, request.BirthDate, request.ChannelType));

        await mgr.SaveChangesAsync(cancellationToken);

        return agg.Aggregate.Id;
    }
}