using LasOneMaybe.Documents;
using MediatR;

namespace LasOneMaybe.Handlers;

public class CheckClientTypeHandler:IRequestHandler<CheckClientType,bool>
{
    public Task<bool> Handle(CheckClientType request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}