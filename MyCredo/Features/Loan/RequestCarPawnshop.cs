using Core.BPM;
using Core.BPM.Interfaces.Builder;
using Core.BPM.MediatR;
using MyCredo.Common;
using MyCredo.Retail.Loan.Application.Features.RequestLoanProcess.CarPawnshop.ConfirmRequestLoan;
using MyCredo.Retail.Loan.Application.Features.RequestLoanProcess.CarPawnshop.Initiating;
using MyCredo.Retail.Loan.Application.Features.TwoFactor.OtpSend;
using MyCredo.Retail.Loan.Application.Features.TwoFactor.OtpValidate;
using MyCredo.Retail.Loan.Domain.Models.LoanApplication.Enums;

namespace MyCredo.Retail.Loan.Application.Features.RequestLoanProcess.CarPawnshop;

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

    public void Apply(ConfirmedRequestLoan @event)
    {
        TraceId = @event.TraceId;
        UserId = @event.UserId;
        Channel = @event.Channel;
        SetBpmProps(@event);
    }
}

public class RequestCarPawnshopDefinition : BpmDefinition<RequestCarPawnshop>
{
    public override void DefineProcess(IProcessBuilder<RequestCarPawnshop> configureProcess)
    {
        configureProcess
            .StartWith<RequestLoanInitiate>()
            .Continue<SendOtp>()
            .Continue<ValidateOtp>()
            .Continue<ConfirmLoanRequest>()
            .Continue<RequestLoanInitiate>();
        //.Continue<FinishCarPawnshop>();
    }
}