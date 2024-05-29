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

public class InitiatePasswordRecoveryHandler(BpmManager<PasswordRecovery> mgr)
    : IRequestHandler<InitiatePasswordRecovery, Guid>
{
    public async Task<Guid> Handle(InitiatePasswordRecovery request, CancellationToken cancellationToken)
    {
        var result = await mgr.StartProcess(PasswordRecovery.Initiate(request.PersonalNumber, request.BirthDate, request.ChannelType), cancellationToken);
        return result.AggregateId;
    }
}