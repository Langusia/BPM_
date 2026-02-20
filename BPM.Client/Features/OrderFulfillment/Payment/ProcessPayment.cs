using Core.BPM.Process;
using Core.BPM.Attributes;
using MediatR;

namespace BPM.Client.Features.OrderFulfillment.Payment;

[BpmProducer(typeof(PaymentProcessed))]
public record ProcessPayment(Guid ProcessId, decimal Amount) : IRequest<bool>;

public class ProcessPaymentHandler(IProcessStore store) : IRequestHandler<ProcessPayment, bool>
{
    public async Task<bool> Handle(ProcessPayment request, CancellationToken cancellationToken)
    {
        var process = await store.FetchProcessAsync(request.ProcessId, cancellationToken);
        var transactionId = Guid.NewGuid().ToString("N");
        process.AppendEvent(new PaymentProcessed(true, request.Amount, transactionId));
        await store.SaveChangesAsync(cancellationToken);
        return true;
    }
}
