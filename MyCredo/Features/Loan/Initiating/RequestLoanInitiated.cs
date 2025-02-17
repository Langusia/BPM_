using Core.BPM.Attributes;

namespace MyCredo.Features.Loan.Initiating;

public record RequestLoanInitiated(DigitalLoanProductTypeEnum ProductType, string? PromoCode, decimal Percent, int? TryCount,decimal Amount, decimal EffectiveInterestRate, int Period) : BpmEvent;
