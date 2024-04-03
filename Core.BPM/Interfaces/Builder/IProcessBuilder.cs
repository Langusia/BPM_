using Core.BPM.Configuration;

namespace Core.BPM.Interfaces.Builder;

public interface IProcessBuilder<TProcess>
{
    public INodeBuilder StartWith<TCommand>()
    {
        var inst = new Node(typeof(TCommand), typeof(TProcess));
        var processInst = new BProcess(typeof(TProcess), inst);
        BProcessGraphConfiguration.AddProcess(processInst);
        return new NodeBuilder(inst, processInst);
    }
}