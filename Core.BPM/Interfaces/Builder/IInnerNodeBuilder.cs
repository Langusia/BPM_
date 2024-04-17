namespace Core.BPM.Interfaces.Builder;

public interface IInnerNodeBuilder : INodeBuilder
{
    INode GetRoot();
    INode SetRoot(INode node);
    INode SetCurrent(INode node);
    IInnerNodeBuilder Continue<Command>(Action<IInnerNodeBuilder>? configure = null);
}