using Core.BPM;
using Core.BPM.MediatR.Attributes;
using MyCredo.Retail.Loan.Application.Features.TwoFactor.OtpValidate;

namespace MyCredo.Features.TwoFactor;

public class TwoFactor : Aggregate
{
    public string Test { get; set; }
    public bool IsValid { get; set; }

    public void Apply(GeneratedOtp @event)
    {
        Test = @event.OtpHash;
        SetBpmProps(@event);
    }

    public void Apply(OtpValidated @event)
    {
        IsValid = @event.ValidOtp;
        SetBpmProps(@event);
    }

    public void GenerateOtp(Guid aggregateId, string otpHash)
    {
        var @event = new GeneratedOtp(aggregateId, otpHash);
        Enqueue(@event);
        Apply(@event);
    }

    public void ValidateOtp(bool isValid)
    {
        var @event = new OtpValidated(Guid.NewGuid(), isValid);
        Enqueue(@event);
        Apply(@event);
    }
}