using Core.BPM.Process;
using Core.BPM.Attributes;
using MediatR;

namespace BPM.Client.Features.OrderFulfillment.Completion;

[BpmProducer(typeof(OrderCompleted))]
public record CompleteOrder(Guid ProcessId) : IRequest;

public class CompleteOrderHandler(IProcessStore store) : IRequestHandler<CompleteOrder>
{
    public async Task Handle(CompleteOrder request, CancellationToken cancellationToken)
    {
        var process = await store.FetchProcessAsync(request.ProcessId, cancellationToken);
        process.AppendEvent(new OrderCompleted());
        await store.SaveChangesAsync(cancellationToken);
    }
}
