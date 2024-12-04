using Core.BPM.Configuration;
using Core.BPM.Nodes;
using MediatR;

namespace Core.BPM.DefinitionBuilder;

public class ProcessBuilder<TProcess> : IProcessBuilder<TProcess> where TProcess : Aggregate
{
    public IProcessNodeInitialBuilder<TProcess> StartWith<TCommand>() where TCommand : IBaseRequest
    {
        var node = new Node(typeof(TCommand), typeof(TProcess));
        var processInst = new BProcess(typeof(TProcess), node);
        BProcessGraphConfiguration.AddProcess(processInst);
        var builder = new ProcessNodeBuilder<TProcess>(node, processInst);
        return builder;
    }
}

public class MyClass<T> where T : Aggregate;