namespace Core.BPM.Interfaces.Builder;

public interface IInnerNodeBuilder : INodeBuilder
{
    INode GetRoot();
    IInnerNodeBuilder Continue<Command>(Action<IInnerNodeBuilder>? configure = null);
}