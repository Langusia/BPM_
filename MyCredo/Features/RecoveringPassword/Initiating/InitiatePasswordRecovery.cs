using Core.BPM.MediatR;
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

public class InitiatePasswordRecoveryHandler(BpmManager mgr1, IDocumentSession ds)
    : IRequestHandler<InitiatePasswordRecovery, Guid>
{
    private readonly BpmManager _mgr = mgr1;
    private readonly IDocumentSession _ds = ds;

    public async Task<Guid> Handle(InitiatePasswordRecovery request, CancellationToken cancellationToken)
    {
        
        var agg = PasswordRecovery.Initiate(request.PersonalNumber, request.BirthDate, request.ChannelType);
        _ds.Events.StartStream<PasswordRecovery>(
            agg.Id,
            agg.DequeueUncommittedEvents()
        );

        await _ds.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return agg.Id;
    }
}