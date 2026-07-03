using BPM.Contracts;
using BPM.Core.Attributes;
using MediatR;

namespace Loan.Contracts;

/// <summary>
/// Loan-origination commands. Annotated for agents with BPM.Contracts
/// attributes; richer metadata lives in the colocated agent specs.
/// </summary>
[BpmProducer(typeof(LoanApplicationInitiated))]
[BpmPolicy(ExecutionPolicy.Autonomous)]
public sealed record InitiateLoanApplication(
    decimal Amount,
    int TermMonths,
    LoanPurpose Purpose,
    UserContext? UserContext = null) : IRequest<Guid>, IAuthenticatedRequest<UserContext>;

[BpmProducer(typeof(CollateralSubmitted))]
[BpmPolicy(ExecutionPolicy.Autonomous)]
[BpmDescription("Registers the customer's vehicle as collateral for the loan.")]
public sealed record SubmitCollateral(
    Guid ProcessId,
    [property: BpmDescription("17-character vehicle identification number")]
    [property: BpmPattern("^[A-HJ-NPR-Z0-9]{17}$")]
    string VehicleVin,
    [property: BpmRange(1990, 2030)] int VehicleYear,
    [property: BpmDescription("Customer's estimate of the vehicle value in GEL")]
    decimal EstimatedValue) : IRequest;

[BpmProducer(typeof(LoanPriced))]
[BpmPolicy(ExecutionPolicy.Autonomous)]
[BpmDescription("Prices the loan. The effective interest rate is computed by the pricing engine.")]
public sealed record PriceLoan(
    Guid ProcessId,
    [property: BpmDerived] decimal? EffectiveInterestRate = null) : IRequest;

// No [BpmPolicy]: demonstrates the RequiresApproval default via the fluent
// spec in SignLoanContractAgentSpec (which also sets it explicitly).
[BpmProducer(typeof(LoanContractSigned))]
public sealed record SignLoanContract(
    Guid ProcessId,
    string? Note,
    UserContext? UserContext = null) : IRequest, IAuthenticatedRequest<UserContext>;

[BpmProducer(typeof(LoanDisbursed))]
[BpmPolicy(ExecutionPolicy.HumanOnly)]
[BpmDescription("Pays the loan out to the customer's account. Never available to agents.")]
public sealed record DisburseLoan(
    Guid ProcessId,
    UserContext? UserContext = null) : IRequest, IAuthenticatedRequest<UserContext>;
