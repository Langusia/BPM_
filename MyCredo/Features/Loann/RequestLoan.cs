using Core.BPM;
using Core.BPM.Application;
using Core.BPM.DefinitionBuilder;
using MyCredo.Common;
using MyCredo.Features.Loan;

namespace MyCredo.Features.Loann;

public class RequestDigitalLoan : Aggregate
{
    public string? PromoCode { get; set; }
    public decimal Percent { get; set; }
    public decimal Amount { get; set; }
    public decimal EffectiveInterestRate { get; set; }
    public int Period { get; set; }
    public InsuranceCompanyEnum Insurance { get; set; }
    public bool NotCheckInRs { get; set; }
    public DigitalLoanProductTypeEnum ProductType { get; set; }
    public ChannelTypeEnum Channel { get; set; }
    public string TraceId { get; set; }
    public int UserId { get; set; }
    public bool HasGracePeriod { get; set; }
    public int? GracePeriod { get; set; }


    public RequestDigitalLoan()
    {
    }

    public RequestDigitalLoan(DigitalLoanInitiated requestLoanInitiated)
    {
        var @event = new DigitalLoanInitiated(
            requestLoanInitiated.ProductType,
            requestLoanInitiated.PromoCode,
            requestLoanInitiated.Percent,
            requestLoanInitiated.Amount,
            requestLoanInitiated.EffectiveInterestRate,
            requestLoanInitiated.Period,
            requestLoanInitiated.Insurance,
            requestLoanInitiated.hasGracePeriod,
            requestLoanInitiated.gracePeriod,
            requestLoanInitiated.NotCheckInRs);
        Apply(@event);
        Enqueue(@event);
    }

    public enum InsuranceCompanyEnum
    {
        Aldagi,
        ImediL,
        Euroins
    }

    public void Initiate(
        DigitalLoanProductTypeEnum productType,
        string? promoCode,
        decimal percent,
        decimal amount,
        decimal effectiveInterestRate,
        int period,
        InsuranceCompanyEnum insurance,
        bool hasGracePeriod,
        int? gracePeriod,
        bool notCheckInRs)
    {
        var @event = new DigitalLoanInitiated(
            productType,
            promoCode,
            percent,
            amount,
            effectiveInterestRate,
            period,
            insurance,
            hasGracePeriod,
            gracePeriod,
            notCheckInRs);
        Apply(@event);
        Enqueue(@event);
    }

    public void Confirm(string traceId, ChannelTypeEnum channel, int userId)
    {
        var @event = new ConfirmedDigitalLoan(traceId, userId, channel);
        Apply(@event);
        Enqueue(@event);
    }

    public void Finish(ChannelTypeEnum channel, int userId)
    {
        var @event = new FinishedRequestDigitalLoan(userId, channel);
        Apply(@event);
        Enqueue(@event);
    }

    public void Apply(DigitalLoanInitiated @event)
    {
        ProductType = @event.ProductType;
        PromoCode = @event.PromoCode;
        Percent = @event.Percent;
        Amount = @event.Amount;
        EffectiveInterestRate = @event.EffectiveInterestRate;
        Period = @event.Period;
        Insurance = @event.Insurance;
        NotCheckInRs = @event.NotCheckInRs;
        SetBpmProps(@event);
    }

    public void Apply(ConfirmedDigitalLoan @event)
    {
        Channel = @event.Channel;
        TraceId = @event.TraceId;
        UserId = @event.UserId;
        SetBpmProps(@event);
    }

    public void Apply(FinishedRequestDigitalLoan @event)
    {
        Channel = @event.Channel;
        UserId = @event.UserId;
        SetBpmProps(@event);
    }
}

public class RequestDigitalLoanDefinition : BpmDefinition<RequestDigitalLoan>
{
    public override void DefineProcess(IProcessBuilder<RequestDigitalLoan> configureProcess)
    {
        configureProcess
            .StartWith<DigitalLoanInitiate>()
            .Continue<ConfirmDigitalLoan>()
            .Continue<FinishDigitalLoan>();
    }
}