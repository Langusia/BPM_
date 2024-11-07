using Core.BPM.Application.Managers;
using Core.BPM.Attributes;
using Credo.Core.Shared.Library;
using Marten;
using MediatR;

namespace MyCredo.Features.RecoveringPassword.CheckingCard;

[BpmProducer(typeof(CheckCardInitiated))]
public record CheckCardInitiate(Guid DocumentId) : IRequest<bool>;

public record CheckCardInitiateHandler(BpmStore<CheckCard, CheckCardInitiate> _bpm, IDocumentSession _session, IQuerySession _qSession) : IRequestHandler<CheckCardInitiate, bool>
{
    private readonly IQuerySession _qSession = _qSession;
    private readonly BpmStore<CheckCard, CheckCardInitiate> _bpm = _bpm;
    private readonly IDocumentSession _session = _session;

    public async Task<bool> Handle(CheckCardInitiate request, CancellationToken cancellationToken)
    {
        var s = await _bpm.AggregateProcessStateAsync(request.DocumentId, cancellationToken);
        if (!s.ValidateOrigin())
            return false;
        //
        //if (s.AppendEvent(x => x.Initiate(0, 0, "hash")) == false)
        //    return false;

        await _bpm.SaveChangesAsync(cancellationToken);
        return true;
    }
}