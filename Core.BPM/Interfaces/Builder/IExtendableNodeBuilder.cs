namespace Core.BPM.Interfaces.Builder;

public interface IExtendableNodeBuilder : INodeBuilder
{
    INode GetRoot();
}