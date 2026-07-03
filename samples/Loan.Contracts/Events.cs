using BPM.Core.Events;

namespace Loan.Contracts;

public sealed record LoanApplicationInitiated(
    string CustomerId,
    string CustomerName,
    decimal Amount,
    int TermMonths,
    LoanPurpose Purpose) : BpmEvent;

public sealed record CollateralSubmitted(
    string VehicleVin,
    int VehicleYear,
    decimal EstimatedValue) : BpmEvent;

public sealed record LoanPriced(decimal EffectiveInterestRate) : BpmEvent;

public sealed record LoanContractSigned(string SignedBy, string? Note) : BpmEvent;

public sealed record LoanDisbursed(string DisbursedBy) : BpmEvent;

public enum LoanPurpose
{
    CarPurchase,
    Refinance,
    WorkingCapital,
    Personal
}
