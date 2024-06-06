using Core.BPM;
using MyCredo.Retail.Loan.Application.Features.TwoFactor.OtpSend;
using MyCredo.Retail.Loan.Application.Features.TwoFactor.OtpValidate;

namespace MyCredo.Retail.Loan.Application.Features.TwoFactor;

public class OtpValidation : Aggregate
{
    public int ValidationCount { get; set; }
    public int SendCount { get; set; }

    public void Apply(OtpSent @event)
    {
        SendCount++;
    }

    public void Apply(OtpValidated @event)
    {
        ValidationCount++;
    }
}