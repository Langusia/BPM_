using Core.BPM.Application.Managers;
using Core.BPM.Attributes;
using Credo.Core.Shared.Library;
using Marten;
using MediatR;

namespace MyCredo.Features.RecoveringPassword.CheckingCard;

[BpmProducer(typeof(CheckCardInitiated))]
public record CheckCardInitiate(Guid DocumentId) : IRequest<bool>;

public record CheckCardInitiateHandler(IDocumentSession _session, IQuerySession _qSession) : IRequestHandler<CheckCardInitiate, bool>
{
    private readonly IQuerySession _qSession = _qSession;
    private readonly IDocumentSession _session = _session;

    public async Task<bool> Handle(CheckCardInitiate request, CancellationToken cancellationToken)
    {
        //
        //if (s.AppendEvent(x => x.Initiate(0, 0, "hash")) == false)
        //    return false;

        return true;
    }
}