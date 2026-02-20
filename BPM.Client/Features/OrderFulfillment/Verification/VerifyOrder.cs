using BPM.Core.Process;
using BPM.Core.Attributes;
using MediatR;

namespace BPM.Client.Features.OrderFulfillment.Verification;

[BpmProducer(typeof(OrderVerified))]
public record VerifyOrder(Guid ProcessId) : IRequest;

public class VerifyOrderHandler(IProcessStore store) : IRequestHandler<VerifyOrder>
{
    public async Task Handle(VerifyOrder request, CancellationToken cancellationToken)
    {
        var process = await store.FetchProcessAsync(request.ProcessId, cancellationToken);
        process.AppendEvent(new OrderVerified(true));
        await store.SaveChangesAsync(cancellationToken);
    }
}
