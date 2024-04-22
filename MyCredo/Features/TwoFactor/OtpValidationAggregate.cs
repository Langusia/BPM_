using Core.BPM;
using Marten.Events.Aggregation;

namespace MyCredo.Features.TwoFactor;

public class OtpValidationAggregate : Aggregate
{
    public Guid ParentAggregateId { get; set; }
    public string HashedOtp { get; set; }
    public bool UserSubmitted { get; set; }
    public bool UserValidated { get; set; }

    public void Apply(GeneratedOtp @event)
    {
        ParentAggregateId = @event.ParentProcessId;
        HashedOtp = @event.OtpHash;
    }

    public void Apply(OtpSubmited @event)
    {
        UserValidated = @event.IsValid;
        HashedOtp = @event.OtpHash;
    }
}

public class OtpValidationProjection : SingleStreamProjection<OtpValidationAggregate>
{
    public Guid Id { get; set; }
    public Guid ParentAggregateId { get; set; }
    public string HashedOtp { get; set; }

    
    public int SubmitCount { get; set; }
    public int SendCount { get; set; }
    public bool Validated { get; set; }

    public void Create(OtpValidationAggregate snapshot, GeneratedOtp @event)
    {
        ParentAggregateId = @event.ParentProcessId;
        HashedOtp = @event.OtpHash;
        SendCount += 1;
    }

    public void Apply(OtpValidationAggregate snapshot, OtpSubmited @event)
    {
        HashedOtp = @event.OtpHash;
        SubmitCount += 1;

        Validated = @event.IsValid;
    }
}