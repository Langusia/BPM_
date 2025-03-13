using Core.BPM;
using MyCredo.Features.Loan.OtpSend;
using MyCredo.Features.Loan.OtpValidate;

namespace MyCredo.Features.Loan;

public class OtpValidation : Aggregate
{
    public int ValidationCount { get; set; }
    public int SendCount { get; set; }
    public bool IsValidOtp { get; set; }

    public override bool? IsCompleted()
    {
        return IsValidOtp;
    }

    public void Apply(OtpSent @event)
    {
        SendCount++;
    }

    public void Apply(OtpValidated @event)
    {
        ValidationCount++;
    }
}