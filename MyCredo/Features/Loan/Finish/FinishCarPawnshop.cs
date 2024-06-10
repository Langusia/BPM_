using Core.BPM.Application.Managers;
using Core.BPM.MediatR.Attributes;
using Credo.Core.Shared.Library;
using Credo.Core.Shared.Mediator;
using MyCredo.Common;

namespace MyCredo.Retail.Loan.Application.Features.RequestLoanProcess.CarPawnshop.Finishing;
[BpmProducer(typeof(FinishedRequestCarPawnshop))]
public record FinishCarPawnshop : ICommand<bool>
{
    public Guid ProcessId { get; set; }
    public ChannelTypeEnum Channel { get; set; }
    public int UserId { get; set; }
}

internal class FinishRequestCarPawnshopHandler
    (
    BpmManager<RequestCarPawnshop> manager
    )
    : ICommandHandler<FinishCarPawnshop, bool>
{
    public async Task<Result<bool>> Handle(FinishCarPawnshop request, CancellationToken cancellationToken)
    {
        var s = await manager.AggregateAsync<FinishCarPawnshop>(request.ProcessId, cancellationToken);


        return Result.Success(true);
    }
}



