using Core.BPM.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Core.BPM.MediatR;

public static class ServiceCollectionExtensions
{
    public static void AddBpm(this IServiceCollection services, Action<Builder> configure)
    {
        services.TryAddScoped(typeof(BpmProcessManager<,>));
        var inst = new Builder(services);
        configure.Invoke(inst);
    }
}

public class Builder
{
    public Builder(IServiceCollection services)
    {
        this.services = services;
    }

    private readonly IServiceCollection services;

    public void AddBpmDefinition<TProcess, TDefinition>() where TProcess : IProcess
        where TDefinition : BpmProcessGraphDefinition<TProcess>
    {
        var definition = (TDefinition)Activator.CreateInstance(typeof(TDefinition))!;
        var configuratorInst = (BpmProcessGraphConfigurator<TProcess>)Activator.CreateInstance(
            typeof(BpmProcessGraphConfigurator<TProcess>))!;


        definition.Define(configuratorInst);
    }
}