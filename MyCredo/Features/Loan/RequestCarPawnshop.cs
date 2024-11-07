using Core.BPM;
using Core.BPM.Application;
using Core.BPM.BCommand;
using Core.BPM.DefinitionBuilder;
using Core.BPM.Extensions;
using MyCredo.Common;
using MyCredo.Features.Loan.ConfirmLoanRequest;
using MyCredo.Features.Loan.Finish;
using MyCredo.Features.Loan.Initiating;
using MyCredo.Features.Loan.OtpSend;
using MyCredo.Features.Loan.OtpValidate;
using MyCredo.Features.Loan.UploadImage;
using MyCredo.Features.Loann;
using MyCredo.Features.RecoveringPassword.Initiating;

namespace MyCredo.Features.Loan;

public class RequestCarPawnshop : Aggregate
{
    public string PromoCode { get; set; }
    public decimal Percent { get; set; }
    public decimal Amount { get; set; }
    public decimal EffectiveInterestRate { get; set; }
    public int Period { get; set; }
    public string Image { get; set; }
    public string Extension { get; set; }
    public string ImageName { get; set; }
    public long CustomerId { get; set; }
    public int ExternalResponseGroup { get; set; }
    public ChannelTypeEnum Channel { get; set; }
    public int UserId { get; set; }
    public int? ExternalProductGroupId { get; set; }
    public string ExternalProductGroupName { get; set; }
    public string TraceId { get; set; }
    public DigitalLoanProductTypeEnum ProductType { get; set; }


    public RequestCarPawnshop()
    {
    }

    public RequestCarPawnshop(RequestLoanInitiated requestLoanInitiated)
    {
        var @event = new RequestLoanInitiated(requestLoanInitiated.ProductType, requestLoanInitiated.PromoCode, requestLoanInitiated.Percent, 0, requestLoanInitiated.Amount,
            requestLoanInitiated.EffectiveInterestRate, requestLoanInitiated.Period);
        Apply(@event);
        Enqueue(@event);
    }

    public static RequestCarPawnshop Initiate(DigitalLoanProductTypeEnum productType, string promoCode, decimal percent, decimal amount, decimal effectiveInterestRate, int period) =>
        new(new RequestLoanInitiated(productType, promoCode, percent, 0, amount, effectiveInterestRate, period));

    public void Apply(RequestLoanInitiated @event)
    {
        ProductType = @event.ProductType;
        PromoCode = @event.PromoCode;
        Percent = @event.Percent;
        Amount = @event.Amount;
        EffectiveInterestRate = @event.EffectiveInterestRate;
        Period = @event.Period;
        SetBpmProps(@event);
    }

    public void InitiateCarPawnshop(DigitalLoanProductTypeEnum productType, string promoCode, decimal percent, int tryCount, decimal amount, decimal effectiveInterestRate, int period)
    {
        var @event = new RequestLoanInitiated(productType, promoCode, percent, 0, amount, effectiveInterestRate, period);
        Apply(@event);
        Enqueue(@event);
    }

    public void Upload()
    {
        var @event = new UploadedImage("", "", "", 0, 0);
        Enqueue(@event);
    }


    public void SendOtp()
    {
        var @event = new OtpSent(Guid.NewGuid());
        Enqueue(@event);
    }

    public void ConfirmedCarPawnshop(string traceId, int userId, ChannelTypeEnum channel)
    {
        var @event = new ConfirmedCarPawnshop(traceId, userId, channel, null);
        Apply(@event);
        Enqueue(@event);
    }

    public void Apply(ConfirmedCarPawnshop @event)
    {
        TraceId = @event.TraceId;
        UserId = @event.UserId;
        Channel = @event.Channel;
        SetBpmProps(@event);
    }
}

public record CarPawnshopInitiated(
    DigitalLoanProductTypeEnum ProductType,
    string? PromoCode,
    decimal Percent,
    decimal Amount,
    decimal EffectiveInterestRate,
    int Period,
    RequestDigitalLoan.InsuranceCompanyEnum? Insurance = null,
    bool NotCheckInRs = false
) : BpmEvent;

public class RequestCarPawnshopDefinition : BpmDefinition<RequestCarPawnshop>
{
    public override void DefineProcess(IProcessBuilder<RequestCarPawnshop> configureProcess)
    {
        configureProcess
            .StartWith<RequestLoanInitiate>()
            .ContinueOptional<SendOtp>()
            .ThenContinueAnyTime<ValidateOtp>()
            .OrOptional<InitiatePasswordRecovery>()
            .Continue<ConfirmLoanRequest.ConfirmLoanRequest>()
            .ThenContinueAnyTime<UploadImage.UploadImage>()
            .Continue<FinishCarPawnshop>();
    }
}