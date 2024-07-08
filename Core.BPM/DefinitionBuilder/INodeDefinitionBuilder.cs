namespace Core.BPM.Interfaces.Builder;

public interface INodeDefinitionBuilder : INodeBuilder
{
    INode GetRoot();
    INode SetRoot(INode node);
    INode SetCurrent(INode node);
    INodeDefinitionBuilder Continue<Command>(Action<INodeDefinitionBuilder>? configure = null);
}

public interface INodeBuilderBuilderModificationBuilder : INodeDefinitionBuilder
{
    INodeBuilderBuilderModificationBuilder AnyTime();
    INodeBuilderBuilderModificationBuilder Optional();
}