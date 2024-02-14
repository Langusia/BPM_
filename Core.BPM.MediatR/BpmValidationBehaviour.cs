using Core.BPM.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Core.BPM.MediatR;

public class BpmValidationBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest, IEvent
{
    
    public BpmValidationBehaviour(IServiceCollection services)
    {
        
    }
    public Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        return null;
    }
}