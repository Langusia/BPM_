namespace BPM.Contracts;

/// <summary>
/// Raw metadata collected from an <see cref="IAgentSpec{TCommand}"/> via
/// <see cref="AgentSpecBuilder{TCommand}"/>. Plain data; merging with attribute
/// and reflection metadata happens in the engine.
/// </summary>
public sealed class AgentSpecData
{
    public LocalizedText? Description { get; set; }
    public ExecutionPolicy? Policy { get; set; }
    public LocalizedText? SuccessCriteria { get; set; }
    public Dictionary<string, AgentFieldData> Fields { get; } = new(StringComparer.Ordinal);

    public AgentFieldData GetOrAddField(string propertyName)
    {
        if (!Fields.TryGetValue(propertyName, out var field))
            Fields[propertyName] = field = new AgentFieldData();
        return field;
    }
}

/// <summary>Per-field metadata collected from a fluent agent spec.</summary>
public sealed class AgentFieldData
{
    public LocalizedText? Description { get; set; }
    public LocalizedText? SourceHint { get; set; }
    public FieldRole? Role { get; set; }
    public bool? Required { get; set; }
    public string? Pattern { get; set; }
    public double? Min { get; set; }
    public double? Max { get; set; }
}
