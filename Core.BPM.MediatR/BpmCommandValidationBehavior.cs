using System.Reflection;
using Core.BPM.MediatR.Mediator;
using Credo.Core.Shared.Library;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Core.BPM.MediatR;

public class BpmRequest<T> : BpmRequest where T : Aggregate
{
    public BpmRequest() : base(typeof(T))
    {
    }
}

public class BpmRequest : Attribute
{
    public BpmRequest(Type processType)
    {
        ProcessType = processType;
    }

    public Type ProcessType;
}

public class BpmCommandValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
// where TRequest : IBpmCommand
    where TResponse : Result

{
    private readonly IServiceProvider _serviceProvider;

    public BpmCommandValidationBehavior(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request!.GetType().GetInterfaces().All(x =>
                x.IsGenericType && x.GetGenericTypeDefinition() != typeof(IBpmRootCommand<>)))
        {
            return await next.Invoke();
        }

        var att = request.GetType().GetCustomAttribute(typeof(BpmRequest<>));
        if (att is null)
            throw new Exception();

        var mgr = (IBpmValidationManager)_serviceProvider.GetRequiredService(
            typeof(BpmProcessManager<>).MakeGenericType(((BpmRequest)att).ProcessType));
        mgr.ValidateConfig<TRequest>();

        if (request!.GetType().GetInterfaces().Any(x =>
                x.IsGenericType &&
                x.GetGenericTypeDefinition() == typeof(IBpmCommand<>)))
        {
            await mgr.ValidateAsync<TRequest>(((IBpmCommand)request).ProcessId, cancellationToken);

            var resultObj = await next.Invoke();

            var result = (Result)resultObj;
            if (result.IsSuccess)
            {
                return resultObj;
            }

            return resultObj;
        }


        return await next.Invoke();
    }
}