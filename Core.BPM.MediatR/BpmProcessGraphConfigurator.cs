using Core.BPM.Configuration;
using Core.BPM.Interfaces;
using Credo.Core.Shared.Library;
using MediatR;

namespace Core.BPM.MediatR;

public abstract class BpmProcessGraphDefinition<TProcess>
    where TProcess : Aggregate
{
    public abstract void Define(BpmProcessGraphConfigurator<TProcess> configure);
}

public class BpmProcessGraphConfigurator<TProcess> where TProcess : Aggregate
{
    public BpmNode<TProcess, T> StartWith<T>() where T : IRequest<Result>
    {
        var inst = new BpmNode<TProcess, T>();
        BpmProcessGraphConfiguration.AddProcess(new BpmProcess<TProcess>(inst));
        return inst;
    }
}

public record NodeConfig
{
    public int PermittedCommandTryCount { get; set; }
}

public static class BpmProcessGraphConfiguratorExtensions
{
    public static INode<TProcess, TCommand> SetConfig<TProcess, TCommand>(
        this INode<TProcess, TCommand> node, Action<NodeConfig<TProcess>> configure)
        where TProcess : Aggregate
    {
        var cfg = new NodeConfig<TProcess>();
        configure.Invoke(cfg);
        node.SetConfig(cfg);
        return node;
    }
}