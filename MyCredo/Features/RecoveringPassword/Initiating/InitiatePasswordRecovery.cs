using Core.BPM.Application.Managers;
using Core.BPM.Attributes;
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

public class InitiatePasswordRecoveryHandler(BpmStore<PasswordRecovery, InitiatePasswordRecovery> mgr, IBpmStore store)
    : IRequestHandler<InitiatePasswordRecovery, Guid>
{
    public async Task<Guid> Handle(InitiatePasswordRecovery request, CancellationToken cancellationToken)
    {
        var process = store.StartProcess<PasswordRecovery>(new PasswordRecoveryInitiated(request.PersonalNumber, request.BirthDate, request.ChannelType));
        await store.SaveChangesAsync(cancellationToken);
        var agg = process.AggregateAs<PasswordRecovery>();
        var aggD = process.AggregateOrDefaultAs<PasswordRecovery>();
        var aggN = process.AggregateOrNullAs<PasswordRecovery>();
        //var agg = mgr.StartProcess(x => x.Initiate(request.PersonalNumber, request.BirthDate, request.ChannelType));
        //agg.AppendEvent(x => x.Initiate(request.PersonalNumber, request.BirthDate, request.ChannelType));
        var s = process.GetNextSteps();

        return agg.Id;
    }
}