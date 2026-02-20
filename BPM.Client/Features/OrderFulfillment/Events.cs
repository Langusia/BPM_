using Core.BPM.Attributes;

namespace BPM.Client.Features.OrderFulfillment;

public record OrderInitiated(
    string CustomerName,
    string ProductSku,
    int Quantity,
    decimal TotalAmount
) : BpmEvent;

public record OrderVerified(bool IsValid) : BpmEvent;

public record PaymentProcessed(bool Success, decimal Amount, string TransactionId) : BpmEvent;

public record OrderShipped(string TrackingNumber) : BpmEvent;

public record OrderCompleted() : BpmEvent;
