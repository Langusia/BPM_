using Core.BPM;

namespace MyCredo.Features.TwoFactor;

public class TwoFactor : Aggregate
{
    public string Test { get; set; }

    public void Apply(GeneratedOtp @event)
    {
        Test = @event.OtpHash;
    }
}