namespace BPM.UI.Schema;

public class BpmSchemaRegistry
{
    private readonly Dictionary<string, CommandSchema> _schemas = new(StringComparer.OrdinalIgnoreCase);

    public void Register(CommandSchema schema)
    {
        _schemas[schema.CommandName] = schema;
    }

    public CommandSchema? GetSchema(string commandName)
    {
        _schemas.TryGetValue(commandName, out var schema);
        return schema;
    }

    public IReadOnlyDictionary<string, CommandSchema> GetAll() => _schemas;
}
