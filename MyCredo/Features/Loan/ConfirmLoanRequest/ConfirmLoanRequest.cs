using Core.BPM.Application.Managers;
using Core.BPM.MediatR.Attributes;
using Credo.Core.Shared.Library;
using Credo.Core.Shared.Mediator;
using MyCredo.Common;

namespace MyCredo.Retail.Loan.Application.Features.RequestLoanProcess.CarPawnshop.ConfirmRequestLoan;

[BpmProducer(typeof(ConfirmedCarPawnshop))]
public record ConfirmLoanRequest(Guid ProcessId, string TraceId, int UserId, ChannelTypeEnum Channel) : ICommand<AggregateResult<bool>>;

public class ConfirmRequestLoanHandler(
    BpmManager<RequestCarPawnshop> manager
) : ICommandHandler<ConfirmLoanRequest, AggregateResult<bool>>
{
    public async Task<Result<AggregateResult<bool>>> Handle(ConfirmLoanRequest request, CancellationToken cancellationToken)
    {
        var aggregateResult = await manager.AggregateAsync<ConfirmLoanRequest>(request.ProcessId, cancellationToken);

        throw new NotImplementedException();
    }
}