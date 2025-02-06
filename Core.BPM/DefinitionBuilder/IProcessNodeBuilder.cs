namespace Core.BPM.DefinitionBuilder;

public interface IProcessNodeBuilder<T> : IProcessBuilder where T : Aggregate
{
    ProcessConfig<T> End(Action<BProcessConfig>? configureProcess = null);
}