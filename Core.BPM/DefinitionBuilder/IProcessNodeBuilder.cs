namespace Core.BPM.DefinitionBuilder;

public interface IProcessNodeBuilder<T> where T : Aggregate
{
    MyClass<T> End();
}