namespace BPM.Contracts;

/// <summary>Agent-facing description for a command (on the record) or a field (on a property).</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class BpmDescriptionAttribute(string description) : Attribute
{
    public string Description { get; } = description;
}

/// <summary>Execution policy tier for a command. Unannotated commands default to <see cref="ExecutionPolicy.RequiresApproval"/>.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class BpmPolicyAttribute(ExecutionPolicy policy) : Attribute
{
    public ExecutionPolicy Policy { get; } = policy;
}

/// <summary>
/// Hint for the agent about where a field's value should come from
/// (e.g. "Customer's stated request; confirm before executing").
/// Surfaces in the projected schema's field description.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class BpmSourceAttribute(string sourceHint) : Attribute
{
    public string SourceHint { get; } = sourceHint;
}

/// <summary>
/// Marks a property as populated by the server from the authenticated transport.
/// Excluded from the agent-facing schema; agent-supplied values are discarded and logged.
/// Properties whose type is a configured identity type are detected without this attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class BpmServerPopulatedAttribute : Attribute;

/// <summary>
/// Marks a property as computed by server-side logic. Excluded from the agent-facing
/// schema; agent-supplied values are discarded and logged.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class BpmDerivedAttribute : Attribute;

/// <summary>Marks an input field as required in the projected schema.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class BpmRequiredAttribute : Attribute;

/// <summary>Regex constraint for a string input field; mapped to a schema pattern keyword.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class BpmPatternAttribute(string pattern) : Attribute
{
    public string Pattern { get; } = pattern;
}

/// <summary>Numeric range constraint for an input field; mapped to schema minimum/maximum keywords.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class BpmRangeAttribute : Attribute
{
    public BpmRangeAttribute(double min, double max)
    {
        Min = min;
        Max = max;
    }

    public double Min { get; }
    public double Max { get; }
}
