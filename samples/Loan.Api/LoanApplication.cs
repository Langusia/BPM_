using BPM.Core.Definition;
using BPM.Core.Definition.Interfaces;
using BPM.Core.Process;
using BPM.Contracts;
using Loan.Contracts;

namespace Loan.Api;

[BpmDescription("Car-pawnshop loan origination: initiate → collateral → pricing → contract signing (approval gated) → disbursement (human only).")]
public class LoanApplication : Aggregate
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int TermMonths { get; set; }
    public LoanPurpose Purpose { get; set; }
    public string? VehicleVin { get; set; }
    public int? VehicleYear { get; set; }
    public decimal? CollateralValue { get; set; }
    public decimal? EffectiveInterestRate { get; set; }
    public bool ContractSigned { get; set; }
    public bool Disbursed { get; set; }

    public override bool? IsCompleted() => Disbursed;

    public void Apply(LoanApplicationInitiated @event)
    {
        CustomerId = @event.CustomerId;
        CustomerName = @event.CustomerName;
        Amount = @event.Amount;
        TermMonths = @event.TermMonths;
        Purpose = @event.Purpose;
    }

    public void Apply(CollateralSubmitted @event)
    {
        VehicleVin = @event.VehicleVin;
        VehicleYear = @event.VehicleYear;
        CollateralValue = @event.EstimatedValue;
    }

    public void Apply(LoanPriced @event)
    {
        EffectiveInterestRate = @event.EffectiveInterestRate;
    }

    public void Apply(LoanContractSigned @event)
    {
        ContractSigned = true;
    }

    public void Apply(LoanDisbursed @event)
    {
        Disbursed = true;
    }
}

public class LoanApplicationDefinition : BpmDefinition<LoanApplication>
{
    public override ProcessConfig<LoanApplication> DefineProcess(IProcessBuilder<LoanApplication> configureProcess) =>
        configureProcess
            .StartWith<InitiateLoanApplication>()
            .Continue<SubmitCollateral>()
            .Continue<PriceLoan>()
            .Continue<SignLoanContract>()
            .Continue<DisburseLoan>()
            .End();
}
