namespace Core.BPM.DefinitionBuilder;

public static class ProcessNodeBuilderExtensions //<TProcess> where TProcess : Aggregate
{
    public static void Or<TCommand>(this BaseNodeDefinition builder)
    {
        return;
    }
}