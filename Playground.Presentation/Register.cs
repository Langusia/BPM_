using Core.BPM;
using Core.BPM.Interfaces;
using MediatR;

namespace Playground.Presentation;

public class Register : IProcess
{
    public int Age { get; set; }
}

public record CheckClientType(Guid DocumentId, int clientId) : IEvent, IRequest<bool>;

public class CheckClientTypeHandler : IRequestHandler<CheckClientType, bool>
{
    public Task<bool> Handle(CheckClientType request, CancellationToken cancellationToken)
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
            .AppendRight<CheckClientType2>(x => x.Age > 3)
            .ThenAppendRight<CheckClientType3>()
            .AppendRight<CheckClientType2>(x => x.Age > 3)
            .AppendRight<CheckClientType2>(x => x.Age > 3)
            ;
    }
}