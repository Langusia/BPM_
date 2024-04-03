using System.Reflection;
using Core.BPM.Interfaces;
using Core.BPM.MediatR.Mediator;
using Core.Persistence;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.BPM.MediatR;

public static class ServiceCollectionExtensions
{
    public static void AddBpm(this IServiceCollection services, Action<Builder> configure = null)
    {
        var inst = new Builder(services);
        configure?.Invoke(inst);
        services.TryAddScoped(typeof(MartenRepository<>));
        services.TryAddScoped(typeof(BpmProcessManager<>));
        //services.TryAddTransient(typeof(IPipelineBehavior<,>), typeof(BpmCommandValidationBehavior<,>));
    }

    public static void AddBpmValidatorPipes(this MediatRServiceConfiguration config)
    {
        config.AddOpenBehavior(typeof(BpmCommandValidationBehavior<,>));
    }
}

public class Builder
{
    public Builder(IServiceCollection services)
    {
        this.services = services;
    }

    private readonly IServiceCollection services;

    public void AddBpmDefinition<TProcess, TDefinition>() where TProcess : Aggregate
        where TDefinition : BpmProcessGraphDefinition<TProcess>
    {
        var definition = (TDefinition)Activator.CreateInstance(typeof(TDefinition))!;
        var configuratorInst = (BpmProcessGraphConfigurator<TProcess>)Activator.CreateInstance(
            typeof(BpmProcessGraphConfigurator<TProcess>))!;


        definition.Define(configuratorInst);
    }
}