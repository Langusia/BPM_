using BPM.Core.Process;
using Loan.Contracts;
using MediatR;

namespace Loan.Api;

public class InitiateLoanApplicationHandler(IProcessStore store)
    : IRequestHandler<InitiateLoanApplication, Guid>
{
    public async Task<Guid> Handle(InitiateLoanApplication request, CancellationToken cancellationToken)
    {
        var identity = request.UserContext
                       ?? throw new InvalidOperationException("UserContext was not populated.");

        var process = store.StartProcess<LoanApplication>(new LoanApplicationInitiated(
            identity.UserId,
            identity.FullName,
            request.Amount,
            request.TermMonths,
            request.Purpose))
            ?? throw new InvalidOperationException("InitiateLoanApplication cannot start a LoanApplication process.");

        await store.SaveChangesAsync(cancellationToken);
        return process.Id;
    }
}

public class SubmitCollateralHandler(IProcessStore store) : IRequestHandler<SubmitCollateral>
{
    public async Task Handle(SubmitCollateral request, CancellationToken cancellationToken)
    {
        var process = await store.FetchProcessAsync(request.ProcessId, cancellationToken);
        var result = process.AppendEvent(new CollateralSubmitted(
            request.VehicleVin, request.VehicleYear, request.EstimatedValue));
        if (!result.IsSuccess)
            throw new InvalidOperationException($"SubmitCollateral rejected: {result.Code}.");

        await store.SaveChangesAsync(cancellationToken);
    }
}

public class PriceLoanHandler(IProcessStore store) : IRequestHandler<PriceLoan>
{
    public async Task Handle(PriceLoan request, CancellationToken cancellationToken)
    {
        var process = await store.FetchProcessAsync(request.ProcessId, cancellationToken);
        var loan = process.AggregateAs<LoanApplication>();

        // Pricing logic owns the rate — a derived field, never agent input.
        var baseRate = 18.0m;
        var loanToValue = loan.CollateralValue is > 0
            ? loan.Amount / loan.CollateralValue.Value
            : 1m;
        var effectiveRate = Math.Round(baseRate + loanToValue * 6m, 2);

        var result = process.AppendEvent(new LoanPriced(effectiveRate));
        if (!result.IsSuccess)
            throw new InvalidOperationException($"PriceLoan rejected: {result.Code}.");

        await store.SaveChangesAsync(cancellationToken);
    }
}

public class SignLoanContractHandler(IProcessStore store) : IRequestHandler<SignLoanContract>
{
    public async Task Handle(SignLoanContract request, CancellationToken cancellationToken)
    {
        var identity = request.UserContext
                       ?? throw new InvalidOperationException("UserContext was not populated.");

        var process = await store.FetchProcessAsync(request.ProcessId, cancellationToken);
        var result = process.AppendEvent(new LoanContractSigned(identity.FullName, request.Note));
        if (!result.IsSuccess)
            throw new InvalidOperationException($"SignLoanContract rejected: {result.Code}.");

        await store.SaveChangesAsync(cancellationToken);
    }
}

public class DisburseLoanHandler(IProcessStore store) : IRequestHandler<DisburseLoan>
{
    public async Task Handle(DisburseLoan request, CancellationToken cancellationToken)
    {
        var identity = request.UserContext
                       ?? throw new InvalidOperationException("UserContext was not populated.");

        var process = await store.FetchProcessAsync(request.ProcessId, cancellationToken);
        var result = process.AppendEvent(new LoanDisbursed(identity.FullName));
        if (!result.IsSuccess)
            throw new InvalidOperationException($"DisburseLoan rejected: {result.Code}.");

        await store.SaveChangesAsync(cancellationToken);
    }
}
