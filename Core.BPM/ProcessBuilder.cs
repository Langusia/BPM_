using Core.BPM.Configuration;
using Core.BPM.Interfaces;
using Core.BPM.Interfaces.Builder;

namespace Core.BPM;

public class ProcessBuilder<TProcess> : IProcessBuilder<TProcess>
{
    public INodeBuilder StartWith<TCommand>()
    {
        var nodeInst = new Node(typeof(TCommand), typeof(TProcess));
        var processInst = new BProcess(typeof(TProcess), nodeInst);
        BProcessGraphConfiguration.AddProcess(processInst);
        return new NodeBuilder(nodeInst, processInst);
    }
}