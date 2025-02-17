namespace Core.BPM.Trash;

public interface IProcessScopedNodeInitialBuilder<TProcess> where TProcess : Aggregate
{
    //IProcessNodeModifierBuilder<TProcess> ThenContinue<TCommand>(Func<IProcessScopedNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null);
    //IProcessNodeModifierBuilder<TProcess> ThenContinueAnyTime<TCommand>(Func<IProcessScopedNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null);
    //IProcessNodeModifierBuilder<TProcess> ThenContinueOptional<TCommand>(Func<IProcessScopedNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null);
    //private ProcessNodeBuilder<TProcess> ThenContinue(ProcessNodeBuilder<TProcess> nextBuilder,
    //    Func<IProcessScopedNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure = null)
    //{
    //    nextBuilder.CurrentBranchInstances.ForEach(x => x.SetPrevSteps(CurrentBranchInstances));
    //    if (configure is not null)
    //    {
    //        var configuredBuilder = (ProcessNodeBuilder<TProcess>)configure.Invoke(nextBuilder);
    //        configuredBuilder._orScopeBuilder = this;
    //
    //        return configuredBuilder;
    //    }
    //
    //    return nextBuilder;
    //}

    //public IProcessNodeModifierBuilder<TProcess> ThenContinue<TCommand>(Func<IProcessScopedNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure)
    //{
    //    return ThenContinue(new ProcessNodeBuilder<TProcess>(new Node(typeof(TCommand), GetProcess().ProcessType), Process, nb), configure);
    //}
    //
    //public IProcessNodeModifierBuilder<TProcess> ThenContinueAnyTime<TCommand>(Func<IProcessScopedNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure)
    //{
    //    return ThenContinue(new ProcessNodeBuilder<TProcess>(new AnyTimeNode(typeof(TCommand), Process.ProcessType), Process, nb), configure);
    //}
    //
    //public IProcessNodeModifierBuilder<TProcess> ThenContinueOptional<TCommand>(Func<IProcessScopedNodeInitialBuilder<TProcess>, IProcessNodeModifierBuilder<TProcess>>? configure)
    //{
    //    return ThenContinue(new ProcessNodeBuilder<TProcess>(new OptionalNode(typeof(TCommand), Process.ProcessType), Process, nb), configure);
    //}
}