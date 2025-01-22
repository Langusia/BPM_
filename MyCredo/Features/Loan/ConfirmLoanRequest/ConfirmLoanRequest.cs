using Core.BPM.Application.Managers;
using Core.BPM.Attributes;
using Credo.Core.Shared.Library;
using Credo.Core.Shared.Mediator;
using MyCredo.Common;

namespace MyCredo.Features.Loan.ConfirmLoanRequest;

[BpmProducer(typeof(ConfirmedCarPawnshop))]
public record ConfirmLoanRequest(Guid ProcessId, string TraceId, int UserId, ChannelTypeEnum Channel) : ICommand<AggregateResult<bool>>;

public class ConfirmRequestLoanHandler(
) : ICommandHandler<ConfirmLoanRequest, AggregateResult<bool>>
{
    public async Task<Result<AggregateResult<bool>>> Handle(ConfirmLoanRequest request, CancellationToken cancellationToken)
    {
        //BL

        //aggregateState.AppendEvent(x => x.ConfirmedCarPawnshop(request.TraceId, request.UserId, request.Channel));

        throw new NotImplementedException();
    }
}