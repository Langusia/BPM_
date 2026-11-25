using BPM.Core.Process;
using BPM.Core.Attributes;
using BPM.Core.Events;
using MediatR;
using MyCredo.Features.Loan.ConfirmLoanRequest;

namespace MyCredo.Features.Loann;

[BpmProducer(typeof(DigitalLoanInitiated))]
public record DigitalLoanInitiate() : IRequest;

[BpmProducer(typeof(ConfirmedDigitalLoan))]
public record ConfirmDigitalLoan() : IRequest;

[BpmProducer(typeof(FinishedRequestDigitalLoan))]
public record FinishDigitalLoan(Guid ProcessId) : IRequest;

public record FinishDigitalLoanHandler() : IRequestHandler<FinishDigitalLoan>
{
    public async Task Handle(FinishDigitalLoan request, CancellationToken cancellationToken)
    {
        return;

        return;
    }
}