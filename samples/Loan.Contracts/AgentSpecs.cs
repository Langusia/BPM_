using BPM.Contracts;

namespace Loan.Contracts;

/// <summary>
/// Agent-grade metadata for <see cref="InitiateLoanApplication"/>. Fluent spec
/// wins over the attributes on the record.
/// </summary>
public sealed class InitiateLoanApplicationAgentSpec : IAgentSpec<InitiateLoanApplication>
{
    public void Configure(AgentSpecBuilder<InitiateLoanApplication> spec)
    {
        spec.Describe("Initiates a car-pawnshop loan application for the authenticated customer.")
            .Policy(ExecutionPolicy.Autonomous)
            .SuccessCriteria("A new loan application process exists and SubmitCollateral is the next step.");

        spec.Field(x => x.Amount)
            .Describe("Requested loan amount in GEL")
            .Source("Customer's stated request; confirm the figure with the customer before executing")
            .Range(500, 50_000);

        spec.Field(x => x.TermMonths)
            .Describe("Repayment term in months")
            .Range(3, 60);

        spec.Field(x => x.Purpose)
            .Describe("What the customer needs the money for");

        spec.Field(x => x.UserContext).ServerPopulated();
    }
}

public sealed class SignLoanContractAgentSpec : IAgentSpec<SignLoanContract>
{
    public void Configure(AgentSpecBuilder<SignLoanContract> spec)
    {
        spec.Describe("Signs the loan contract on behalf of the bank. This commits the bank to the loan.")
            .Policy(ExecutionPolicy.RequiresApproval)
            .SuccessCriteria("The contract is signed and the loan is ready for disbursement.");

        spec.Field(x => x.Note)
            .Describe("Optional note recorded with the signature");

        spec.Field(x => x.UserContext).ServerPopulated();
    }
}
