namespace Core.BPM.DefinitionBuilder;

public interface IProcessNodeScopedInitialBuilder<TProcess> where TProcess : Aggregate
{
    INodeDefinitionBuilder ThenContinueOptional<TCommand>(Action<INodeDefinitionBuilder>? configure = null);
    INodeDefinitionBuilder ThenContinueAnyTime<TCommand>(Action<INodeDefinitionBuilder>? configure = null);
    INodeDefinitionBuilder ThenContinue<TCommand>(Action<INodeDefinitionBuilder>? configure = null);
}