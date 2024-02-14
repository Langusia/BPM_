using Core.BPM.Interfaces;

namespace Core.BPM;

public class BpmProcess<TProcess>(INode rootNode) : BpmProcess(typeof(TProcess), rootNode) where TProcess : IProcess
{
}

public class BpmProcess(Type processType, INode rootNode)
{
    public Type ProcessType { get; } = processType;
    public INode RootNode { get; } = rootNode;
}