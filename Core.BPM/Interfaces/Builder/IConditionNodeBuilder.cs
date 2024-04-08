namespace Core.BPM.Interfaces.Builder;

public interface IOuterNodeBuilderBuilder : INodeBuilder
{
    INode GetRoot();
    IOuterNodeBuilderBuilder Continue<Command>(Action<IInnerNodeBuilder>? configure = null);
}