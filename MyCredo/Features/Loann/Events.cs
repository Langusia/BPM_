using Core.BPM.BCommand;
using MyCredo.Common;
using MyCredo.Features.Loan;
using MyCredo.Features.Loann;

namespace MyCredo.Features.Loann;

public record FinishedRequestDigitalLoan(int UserId, ChannelTypeEnum Channel) : BpmEvent;

public record ConfirmedDigitalLoan(string TraceId, int UserId, ChannelTypeEnum Channel) : BpmEvent;

public interface IInterface
{
    string? PromoCode { get; init; }
}

public record DigitalLoanInitiated(
    DigitalLoanProductTypeEnum ProductType,
    string? PromoCode,
    decimal Percent,
    decimal Amount,
    decimal EffectiveInterestRate,
    int Period,
    RequestDigitalLoan.InsuranceCompanyEnum Insurance,
    bool hasGracePeriod,
    int? gracePeriod,
    bool NotCheckInRs = false
) : BpmEvent, IInterface;