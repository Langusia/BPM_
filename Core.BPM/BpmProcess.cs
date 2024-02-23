using Core.BPM.Interfaces;

namespace Core.BPM;

public class BpmProcess<TProcess>(INode<TProcess> rootNode)
    : BpmProcess(typeof(TProcess), rootNode) where TProcess : IProcess
{
    public INode<TProcess> RootNode { get; } = rootNode;
}

public class BpmProcess(Type processType, INode rootNode)
{
    public Type ProcessType { get; } = processType;
    public INode RootNode { get; } = rootNode;
}