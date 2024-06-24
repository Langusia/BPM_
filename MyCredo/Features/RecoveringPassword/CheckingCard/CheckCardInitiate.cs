using Core.BPM.Application.Managers;
using Core.BPM.MediatR.Attributes;
using Core.BPM.MediatR.Managers;
using Credo.Core.Shared.Library;
using Marten;
using MediatR;

namespace MyCredo.Features.RecoveringPassword.CheckingCard;

[BpmProducer(typeof(CheckCardInitiated))]
public record CheckCardInitiate(Guid DocumentId) : IRequest<bool>;

public record CheckCardInitiateHandler(BpmManager<CheckCard> _bpm, IDocumentSession _session, IQuerySession _qSession) : IRequestHandler<CheckCardInitiate, bool>
{
    private readonly IQuerySession _qSession = _qSession;
    private readonly BpmManager<CheckCard> _bpm = _bpm;
    private readonly IDocumentSession _session = _session;

    public async Task<bool> Handle(CheckCardInitiate request, CancellationToken cancellationToken)
    {
        var s = await _bpm.AggregateAsync<CheckCardInitiate>(request.DocumentId, cancellationToken);
        await _bpm.AppendAsync<CheckCardInitiate>(request.DocumentId, [new CheckCardInitiated(22, 34433, "hash")], cancellationToken);
        //await _session.LoadAsync<CheckCard>(request.DocumentId, cancellationToken);
        return true;
    }
}