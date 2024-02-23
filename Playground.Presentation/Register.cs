using Core.BPM;
using Core.BPM.Configuration;
using Core.BPM.Interfaces;
using Core.BPM.MediatR;
using Credo.Core.Shared.Library;
using MediatR;

namespace Playground.Presentation;

public class Register : Aggregate
{
    public int Age { get; set; }
}

public record CheckClientType(Guid DocumentId, int clientId) : IEvent, IRequest<Result<bool>>;

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

public record CheckClientType2(Guid DocumentId, int clientId) : IEvent;

public record CheckClientType3(Guid DocumentId, int clientId) : IEvent;

public class RegisterDefinition : BpmProcessGraphDefinition<Register>
{
    public override void Define(BpmProcessGraphConfigurator<Register> configurator)
    {
        configurator.SetRootNode<CheckClientType>()
            .AppendRight<CheckClientType2>(x => x.Age > 3);
    }
}