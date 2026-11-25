using BPM.Core.Process;
using BPM.Core.Attributes;
using BPM.Core.Events;
using Credo.Core.Shared.Library;
using Marten;
using MediatR;
using MyCredo.Features.TwoFactor;

namespace MyCredo.Features.RecoveringPassword.CheckingCard;

[BpmProducer(typeof(Cd))]
public record CheckCardInitiate(Guid DocumentId) : IRequest<bool>;

public record CheckCardInitiateHandler(IProcessStore store) : IRequestHandler<CheckCardInitiate, bool>
{
    public async Task<bool> Handle(CheckCardInitiate request, CancellationToken cancellationToken)
    {
        var process = await store.FetchProcessAsync(request.DocumentId, cancellationToken);
        var res = process!.AppendEvent(new Cd(Guid.Empty, true));

        var nexts = process.GetNextSteps();

        await store.SaveChangesAsync(cancellationToken);

        //process!.AppendEvent(new Fd(Guid.Empty));
        return true;
    }
}