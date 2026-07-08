using System.Linq.Expressions;

namespace BPM.Contracts;

/// <summary>
/// Fluent builder used inside <see cref="IAgentSpec{TCommand}.Configure"/>.
/// Collected data wins over attribute annotations, which win over reflection defaults.
/// </summary>
public sealed class AgentSpecBuilder<TCommand>
{
    public AgentSpecData Data { get; } = new();

    public AgentSpecBuilder<TCommand> Describe(string description)
    {
        (Data.Description ??= new LocalizedText()).Set(LocalizedText.DefaultLanguage, description);
        return this;
    }

    /// <summary>Adds a language-specific description (ISO 639-1 code, e.g. "ka"). Chain calls to add variants.</summary>
    public AgentSpecBuilder<TCommand> Describe(string language, string description)
    {
        (Data.Description ??= new LocalizedText()).Set(language, description);
        return this;
    }

    public AgentSpecBuilder<TCommand> Policy(ExecutionPolicy policy)
    {
        Data.Policy = policy;
        return this;
    }

    /// <summary>What a successful execution looks like, phrased for the agent.</summary>
    public AgentSpecBuilder<TCommand> SuccessCriteria(string criteria)
    {
        (Data.SuccessCriteria ??= new LocalizedText()).Set(LocalizedText.DefaultLanguage, criteria);
        return this;
    }

    /// <summary>Adds language-specific success criteria (ISO 639-1 code, e.g. "ka").</summary>
    public AgentSpecBuilder<TCommand> SuccessCriteria(string language, string criteria)
    {
        (Data.SuccessCriteria ??= new LocalizedText()).Set(language, criteria);
        return this;
    }

    public AgentFieldBuilder Field<TProperty>(Expression<Func<TCommand, TProperty>> selector)
    {
        var propertyName = GetPropertyName(selector);
        return new AgentFieldBuilder(Data.GetOrAddField(propertyName));
    }

    private static string GetPropertyName<TProperty>(Expression<Func<TCommand, TProperty>> selector)
    {
        var body = selector.Body;
        if (body is UnaryExpression { NodeType: ExpressionType.Convert } unary)
            body = unary.Operand;
        if (body is MemberExpression member)
            return member.Member.Name;
        throw new ArgumentException(
            $"Field selector for {typeof(TCommand).Name} must be a simple property access, e.g. x => x.Amount.",
            nameof(selector));
    }
}

/// <summary>Fluent builder for a single field's agent-grade metadata.</summary>
public sealed class AgentFieldBuilder
{
    private readonly AgentFieldData _data;

    internal AgentFieldBuilder(AgentFieldData data) => _data = data;

    public AgentFieldBuilder Describe(string description)
    {
        (_data.Description ??= new LocalizedText()).Set(LocalizedText.DefaultLanguage, description);
        return this;
    }

    /// <summary>Adds a language-specific field description (ISO 639-1 code, e.g. "ka").</summary>
    public AgentFieldBuilder Describe(string language, string description)
    {
        (_data.Description ??= new LocalizedText()).Set(language, description);
        return this;
    }

    /// <summary>Where the value should come from (e.g. "Customer's stated request; confirm before executing").</summary>
    public AgentFieldBuilder Source(string sourceHint)
    {
        (_data.SourceHint ??= new LocalizedText()).Set(LocalizedText.DefaultLanguage, sourceHint);
        return this;
    }

    /// <summary>Adds a language-specific source hint (ISO 639-1 code, e.g. "ka").</summary>
    public AgentFieldBuilder Source(string language, string sourceHint)
    {
        (_data.SourceHint ??= new LocalizedText()).Set(language, sourceHint);
        return this;
    }

    public AgentFieldBuilder Required(bool required = true)
    {
        _data.Required = required;
        return this;
    }

    public AgentFieldBuilder Pattern(string regex)
    {
        _data.Pattern = regex;
        return this;
    }

    public AgentFieldBuilder Range(double? min, double? max)
    {
        _data.Min = min;
        _data.Max = max;
        return this;
    }

    /// <summary>Computed server-side; excluded from the agent-facing schema.</summary>
    public AgentFieldBuilder Derived()
    {
        _data.Role = FieldRole.Derived;
        return this;
    }

    /// <summary>Populated from the authenticated transport; excluded from the agent-facing schema.</summary>
    public AgentFieldBuilder ServerPopulated()
    {
        _data.Role = FieldRole.ServerPopulated;
        return this;
    }
}
