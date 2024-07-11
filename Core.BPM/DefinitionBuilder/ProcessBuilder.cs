using Core.BPM.Configuration;
using Core.BPM.Nodes;
using MediatR;

namespace Core.BPM.DefinitionBuilder;

public class ProcessBuilder<TProcess> : IProcessBuilder<TProcess>
{
    public INodeBuilder StartWith<TCommand>() where TCommand : IBaseRequest
    {
        var nodeInst = new Node(typeof(TCommand), typeof(TProcess));
        var processInst = new BProcess(typeof(TProcess), nodeInst);
        BProcessGraphConfiguration.AddProcess(processInst);
        return new NodeBuilder(nodeInst, processInst);
    }
}