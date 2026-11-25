using BPM.Core.Attributes;
using BPM.Core.Events;

namespace MyCredo.Features.Loan.Initiating;

public record RequestLoanInitiated(DigitalLoanProductTypeEnum ProductType, string? PromoCode, decimal Percent, int? TryCount,decimal Amount, decimal EffectiveInterestRate, int Period) : BpmEvent;
