using Core.BPM.Application.Managers;
using Core.BPM.Attributes;
using MediatR;

namespace BPM.Client.Features.OrderFulfillment.Shipping;

[BpmProducer(typeof(OrderShipped))]
public record ShipOrder(Guid ProcessId) : IRequest;

public class ShipOrderHandler(IBpmStore store) : IRequestHandler<ShipOrder>
{
    public async Task Handle(ShipOrder request, CancellationToken cancellationToken)
    {
        var process = await store.FetchProcessAsync(request.ProcessId, cancellationToken);
        var trackingNumber = $"TRK-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
        process.AppendEvent(new OrderShipped(trackingNumber));
        await store.SaveChangesAsync(cancellationToken);
    }
}
