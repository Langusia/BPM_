namespace Core.BPM.DefinitionBuilder;

public interface IProcessNodeBuilder<T> : IProcessBuilder where T : Aggregate
{
    MyClass<T> End();
}