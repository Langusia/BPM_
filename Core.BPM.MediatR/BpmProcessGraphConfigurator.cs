using Core.BPM.Configuration;
using Core.BPM.Interfaces;
using Credo.Core.Shared.Library;
using Credo.Core.Shared.Mediator;
using MediatR;

namespace Core.BPM.MediatR;

public abstract class BpmProcessGraphDefinition<TProcess>
    where TProcess : IProcess
{
    public abstract void Define(BpmProcessGraphConfigurator<TProcess> configure);
}

public class BpmProcessGraphConfigurator<TProcess> where TProcess : IProcess
{
    public CredoBpmNode<TProcess, T> SetRootNode<T>() where T : IRequest<Result>
    {
        var inst = new CredoBpmNode<TProcess, T>();
        BpmProcessGraphConfiguration.AddProcess(new BpmProcess(typeof(TProcess), inst));
        return inst;
    }
}