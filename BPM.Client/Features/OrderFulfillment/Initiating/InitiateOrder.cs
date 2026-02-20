using BPM.Core.Process;
using BPM.Core.Attributes;
using BPM.Core.Events;
using MediatR;

namespace BPM.Client.Features.OrderFulfillment.Initiating;

[BpmProducer(typeof(OrderInitiated))]
public record InitiateOrder(string CustomerName, string ProductSku, int Quantity, decimal TotalAmount) : IRequest<Guid>;

public class InitiateOrderHandler(IProcessStore store) : IRequestHandler<InitiateOrder, Guid>
{
    public async Task<Guid> Handle(InitiateOrder request, CancellationToken cancellationToken)
    {
        var process = store.StartProcess<OrderFulfillment>(
            new OrderInitiated(request.CustomerName, request.ProductSku, request.Quantity, request.TotalAmount));
        await store.SaveChangesAsync(cancellationToken);
        return process.Id;
    }
}
