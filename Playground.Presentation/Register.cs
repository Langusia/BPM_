using Core.BPM;
using Core.BPM.Configuration;
using Core.BPM.Interfaces;
using Credo.Core.Shared.Library;
using MediatR;

namespace Playground.Presentation;

public class Register : Aggregate
{
    public int Age { get; set; }
}

public record CheckClientType(Guid DocumentId, int clientId) : IBpmEvent, IRequest<Result<bool>>;

public class CheckClientTypeHandler : IRequestHandler<CheckClientType, Result<bool>>
{
    public Task<Result<bool>> Handle(CheckClientType request, CancellationToken cancellationToken)
    {
        //var process = getProcess
        //var events = process.events
        //checkChainPosition
        //return error

        //a;sldka;lskd

        //return next nodes
        return null;
    }
}

public record CheckClientType2(Guid DocumentId, int clientId) : IBpmEvent;

public record CheckClientType3(Guid DocumentId, int clientId) : IBpmEvent;