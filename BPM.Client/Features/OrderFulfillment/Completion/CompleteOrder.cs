using Core.BPM.Application.Managers;
using Core.BPM.Attributes;
using MediatR;

namespace BPM.Client.Features.OrderFulfillment.Completion;

[BpmProducer(typeof(OrderCompleted))]
public record CompleteOrder(Guid ProcessId) : IRequest;

public class CompleteOrderHandler(IBpmStore store) : IRequestHandler<CompleteOrder>
{
    public async Task Handle(CompleteOrder request, CancellationToken cancellationToken)
    {
        var process = await store.FetchProcessAsync(request.ProcessId, cancellationToken);
        process.AppendEvent(new OrderCompleted());
        await store.SaveChangesAsync(cancellationToken);
    }
}
