using BPM.Core.Process;
using BPM.Core.Definition;
using BPM.Core.Definition.Interfaces;
using BPM.Client.Features.OrderFulfillment.Initiating;
using BPM.Client.Features.OrderFulfillment.Verification;
using BPM.Client.Features.OrderFulfillment.Payment;
using BPM.Client.Features.OrderFulfillment.Shipping;
using BPM.Client.Features.OrderFulfillment.Completion;

namespace BPM.Client.Features.OrderFulfillment;

public class OrderFulfillment : Aggregate
{
    public string CustomerName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal TotalAmount { get; set; }
    public bool IsVerified { get; set; }
    public bool IsPaid { get; set; }
    public bool IsShipped { get; set; }
    public string? TrackingNumber { get; set; }
    public OrderStatus Status { get; set; }

    public void Apply(OrderInitiated @event)
    {
        CustomerName = @event.CustomerName;
        ProductSku = @event.ProductSku;
        Quantity = @event.Quantity;
        TotalAmount = @event.TotalAmount;
        Status = OrderStatus.Initiated;
    }

    public void Apply(OrderVerified @event)
    {
        IsVerified = @event.IsValid;
        Status = OrderStatus.Verified;
    }

    public void Apply(PaymentProcessed @event)
    {
        IsPaid = @event.Success;
        Status = OrderStatus.Paid;
    }

    public void Apply(OrderShipped @event)
    {
        IsShipped = true;
        TrackingNumber = @event.TrackingNumber;
        Status = OrderStatus.Shipped;
    }

    public void Apply(OrderCompleted @event)
    {
        Status = OrderStatus.Completed;
    }

    public void Initiate(string customerName, string productSku, int quantity, decimal totalAmount)
    {
        var @event = new OrderInitiated(customerName, productSku, quantity, totalAmount);
        Apply(@event);
        Enqueue(@event);
    }

    public void Verify(bool isValid)
    {
        var @event = new OrderVerified(isValid);
        Apply(@event);
        Enqueue(@event);
    }

    public void ProcessPayment(decimal amount, string transactionId)
    {
        var @event = new PaymentProcessed(true, amount, transactionId);
        Apply(@event);
        Enqueue(@event);
    }

    public void Ship(string trackingNumber)
    {
        var @event = new OrderShipped(trackingNumber);
        Apply(@event);
        Enqueue(@event);
    }

    public void Complete()
    {
        var @event = new OrderCompleted();
        Apply(@event);
        Enqueue(@event);
    }
}

public enum OrderStatus
{
    Initiated,
    Verified,
    Paid,
    Shipped,
    Completed
}

public class OrderFulfillmentDefinition : BpmDefinition<OrderFulfillment>
{
    public override ProcessConfig<OrderFulfillment> DefineProcess(IProcessBuilder<OrderFulfillment> configureProcess)
    {
        return configureProcess
            .StartWith<InitiateOrder>()
            .Continue<VerifyOrder>()
            .Continue<ProcessPayment>()
            .If(x => x.IsPaid, branch =>
                branch.UnlockOptional<ShipOrder>())
            .Continue<CompleteOrder>()
            .End();
    }

}
