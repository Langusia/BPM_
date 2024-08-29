using Core.BPM.Application.Managers;
using Core.BPM.Attributes;
using MediatR;
using MyCredo.Features.Loan.ConfirmLoanRequest;

namespace MyCredo.Features.Loann;

[BpmProducer(typeof(DigitalLoanInitiated))]
public record DigitalLoanInitiate() : IRequest;

[BpmProducer(typeof(ConfirmedDigitalLoan))]
public record ConfirmDigitalLoan() : IRequest;

[BpmProducer(typeof(FinishedRequestDigitalLoan))]
public record FinishDigitalLoan(Guid ProcessId) : IRequest;

public record FinishDigitalLoanHandler(BpmStore<RequestDigitalLoan, FinishDigitalLoan> manager) : IRequestHandler<FinishDigitalLoan>
{
    private BpmStore<RequestDigitalLoan, FinishDigitalLoan> manager = manager;

    public async Task Handle(FinishDigitalLoan request, CancellationToken cancellationToken)
    {
        var aggregateState = await manager.AggregateProcessStateAsync(request.ProcessId, cancellationToken);
        var matched = aggregateState.ValidateFor<ConfirmLoanRequest>();
        if (!matched)
            return;

        return;
    }
}