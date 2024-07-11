﻿using Core.BPM.Application.Managers;
using Core.BPM.Attributes;
using Credo.Core.Shared.Library;
using Credo.Core.Shared.Mediator;
using MyCredo.Common;

namespace MyCredo.Features.Loan.Finish;

[BpmProducer(typeof(FinishedRequestCarPawnshop))]
public record FinishCarPawnshop : ICommand<bool>
{
    public Guid ProcessId { get; set; }
    public ChannelTypeEnum Channel { get; set; }
    public int UserId { get; set; }
}

internal class FinishRequestCarPawnshopHandler(
    BpmStore<RequestCarPawnshop, FinishCarPawnshop> manager
)
    : ICommandHandler<FinishCarPawnshop, bool>
{
    public async Task<Result<bool>> Handle(FinishCarPawnshop request, CancellationToken cancellationToken)
    {
        var s = await manager.AggregateProcessStateAsync(request.ProcessId, cancellationToken);


        return Result.Success(true);
    }
}