using Core.BPM.Configuration;

namespace Core.BPM.Interfaces.Builder;

public interface IProcessBuilder<TProcess> : IProcessBuilder
{
    public INodeBuilderBuilder StartWith<TCommand>() => StartWith(typeof(TProcess), typeof(TCommand));
}

public interface IProcessBuilder
{
    public INodeBuilderBuilder StartWith(Type processType, Type commandType)
    {
        var inst = new Node(commandType, processType);
        var processInst = new BProcess(processType, inst);
        BProcessGraphConfiguration.AddProcess(processInst);
        return new NodeBuilder(inst, processInst);
    }
}