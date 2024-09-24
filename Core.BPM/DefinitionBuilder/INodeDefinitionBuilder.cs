using Core.BPM.Interfaces;

namespace Core.BPM.DefinitionBuilder;

public interface INodeDefinitionBuilder : INodeBuilder
{
    INode GetRoot();
    INode SetRoot(INode node);
    INode SetCurrent(INode node);
    INodeDefinitionBuilder Continue<Command>(Action<INodeDefinitionBuilder>? configure = null);
    INodeDefinitionBuilder ContinueAnyTime<Command>(Action<INodeDefinitionBuilder>? configure = null);
    INodeDefinitionBuilder ContinueOptional<Command>(Action<INodeDefinitionBuilder>? configure = null);
}

public interface INodeBuilderBuilderModificationBuilder : INodeDefinitionBuilder
{
    INodeBuilderBuilderModificationBuilder AnyTime();
    INodeBuilderBuilderModificationBuilder Optional();
}