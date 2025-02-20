using Core.BPM;
using MyCredo.Features.Loan.OtpValidate;
using MyCredo.Features.RecoveringPassword.Finishing;

namespace MyCredo.Features.TwoFactor;

public class TwoFactor : Aggregate
{
    public string Test { get; set; }
    public bool IsValid { get; set; }

    public void Apply(OtpSent @event)
    {
        Test = @event.OtpHash;
    }

    public void Apply(OtpValidated @event)
    {
        IsValid = @event.ValidOtp;
    }

    public void GenerateOtp(Guid aggregateId, string otpHash)
    {
        var @event = new OtpSent(aggregateId, otpHash);
        Enqueue(@event);
        Apply(@event);
    }

    public void Finish(Guid aggregateId, string otpHash)
    {
        var @event = new FinishedPasswordRecovery();
        Enqueue(@event);
    }

    public void ValidateOtp(bool isValid)
    {
        var @event = new OtpValidated(Guid.NewGuid(), isValid);
        Enqueue(@event);
        Apply(@event);
    }
}