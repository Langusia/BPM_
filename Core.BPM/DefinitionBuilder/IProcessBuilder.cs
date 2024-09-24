using Core.BPM.Configuration;
using Core.BPM.Nodes;
using MediatR;

namespace Core.BPM.DefinitionBuilder;

public interface IProcessBuilder<TProcess> : IProcessBuilder
{
    public INodeDefinitionBuilder StartWith<TCommand>() where TCommand : IBaseRequest => StartWith(typeof(TProcess), typeof(TCommand));
}

public interface IProcessBuilder
{
    public INodeDefinitionBuilder StartWith(Type processType, Type commandType, Action<INodeDefinitionBuilder>? action = null)
    {
        var node = new Node(commandType, processType);
        var processInst = new BProcess(processType, node);
        BProcessGraphConfiguration.AddProcess(processInst);
        var builder = new NodeBuilder(node, processInst);
        action?.Invoke(builder);
        return builder;
    }
}