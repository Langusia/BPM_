using Core.BPM.Application.Managers;
using Core.BPM.Attributes;
using Credo.Core.Shared.Library;
using Credo.Core.Shared.Mediator;

namespace MyCredo.Features.Loan.Initiating;

[BpmProducer(typeof(RequestLoanInitiated))]
public record RequestLoanInitiate : ICommand<RequestLoanInitiateResponse>
{
    public DigitalLoanProductTypeEnum ProductType { get; set; }
    public string? PromoCode { get; set; }
    public decimal Percent { get; set; }
    public decimal Amount { get; set; }
    public decimal EffectiveInterestRate { get; set; }
    public int Period { get; set; }
    public int UserId { get; set; }
    public string TraceId { get; set; }
}

public class RequestLoanInitiateHandler(BpmStore<RequestCarPawnshop, RequestLoanInitiate> manager) : ICommandHandler<RequestLoanInitiate, RequestLoanInitiateResponse>
{
    public async Task<Result<RequestLoanInitiateResponse>> Handle(RequestLoanInitiate request, CancellationToken cancellationToken)
    {
        var agg =
            await manager.StartProcess(x =>
                x.InitiateCarPawnshop(request.ProductType, request.PromoCode, request.Percent, 0, request.Amount, request.EffectiveInterestRate, request.Period), cancellationToken);

        throw new NotImplementedException();
    }
}