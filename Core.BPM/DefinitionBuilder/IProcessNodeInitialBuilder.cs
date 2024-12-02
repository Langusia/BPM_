using Core.BPM.Interfaces;

namespace Core.BPM.DefinitionBuilder;

public interface IProcessNodeInitialBuilder<out TProcess> : INodeDefinitionBuilder where TProcess : Aggregate
{
    IProcessNodeModifierBuilder<TProcess> Case(Predicate<TProcess> predicate, Action<IProcessNodeInitialBuilder<TProcess>> configure);
    IProcessNodeModifierBuilder<TProcess> Continue<Command>(Action<IProcessScopedNodeInitialBuilder<TProcess>>? configure = null);
    IProcessNodeModifierBuilder<TProcess> ContinueAnyTime<Command>(Action<IProcessScopedNodeInitialBuilder<TProcess>>? configure = null);
    IProcessNodeModifierBuilder<TProcess> ContinueOptional<Command>(Action<IProcessScopedNodeInitialBuilder<TProcess>>? configure = null);
}

public interface INodeDefinitionBuilder
{
    INode GetRoot();
    INode SetRoot(INode node);
    INode SetCurrent(INode node);
    BProcess GetProcess();
    INode GetCurrent();
}