namespace BPM.UI.Configuration;

public class BpmUiConfiguration
{
    private BpmConventionBuilder? _conventionBuilder;
    private readonly Dictionary<Type, Dictionary<Type, string>> _aggregateCommandMaps = [];
    private readonly Dictionary<Type, string> _commandMaps = [];
    private string _defaultEndpointPattern = "/bpm/execute/{0}";

    public BpmUiConfiguration UseConvention(Func<BpmConventionBuilder, BpmConventionBuilder> configure)
    {
        _conventionBuilder = configure(new BpmConventionBuilder());
        return this;
    }

    public BpmUiConfiguration MapAggregate<TAggregate>(Action<AggregateCommandMapper> configure)
    {
        var mapper = new AggregateCommandMapper();
        configure(mapper);
        _aggregateCommandMaps[typeof(TAggregate)] = mapper.GetMappings();
        return this;
    }

    public BpmUiConfiguration MapCommand<TCommand>(string endpoint)
    {
        _commandMaps[typeof(TCommand)] = endpoint;
        return this;
    }

    internal string ResolveEndpoint(string commandName, Type? aggregateType)
    {
        var commandType = FindCommandType(commandName);

        if (commandType is not null && _commandMaps.TryGetValue(commandType, out var directEndpoint))
            return directEndpoint;

        if (aggregateType is not null && commandType is not null &&
            _aggregateCommandMaps.TryGetValue(aggregateType, out var aggregateMap) &&
            aggregateMap.TryGetValue(commandType, out var aggregateEndpoint))
            return aggregateEndpoint;

        if (_conventionBuilder is not null)
            return _conventionBuilder.Apply(commandName);

        return string.Format(_defaultEndpointPattern, commandName);
    }

    private static Type? FindCommandType(string commandName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var type = assembly.GetTypes().FirstOrDefault(t => t.Name == commandName);
                if (type is not null) return type;
            }
            catch
            {
                // Skip assemblies that can't be reflected
            }
        }
        return null;
    }
}

public class AggregateCommandMapper
{
    private readonly Dictionary<Type, string> _mappings = [];

    public AggregateCommandMapper Map<TCommand>(string endpoint)
    {
        _mappings[typeof(TCommand)] = endpoint;
        return this;
    }

    internal Dictionary<Type, string> GetMappings() => _mappings;
}
