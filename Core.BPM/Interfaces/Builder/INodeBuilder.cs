namespace Core.BPM.Interfaces.Builder;

public interface INodeBuilder
{
    BProcess GetProcess();
    INode GetCurrent();
    IExtendableNodeBuilder Continue<Command>(Func<NodeBuilder, IExtendableNodeBuilder>? configure = null);
}