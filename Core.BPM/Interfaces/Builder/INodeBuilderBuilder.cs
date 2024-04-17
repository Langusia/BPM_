namespace Core.BPM.Interfaces.Builder;

public interface INodeBuilderBuilder : INodeBuilder
{
    INode GetRoot();
    INode SetRoot(INode node);
    INode SetCurrent(INode node);
    INodeBuilderBuilder Continue<Command>(Action<INodeBuilderBuilder>? configure = null);
}