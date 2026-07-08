using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BPM.Contracts;
using BPM.Core.Application.Identity;

namespace BPM.Core.Application.Metadata;

/// <summary>
/// Merges agent-grade metadata for a command with strict precedence:
/// fluent agent spec &gt; attributes &gt; reflection defaults.
/// Classifies every property into exactly one <see cref="FieldRole"/> bucket.
/// </summary>
public sealed class CommandMetadataResolver(AgentSpecRegistry specs, BpmIdentityOptions identity, ExecutionPolicy defaultPolicy = ExecutionPolicy.RequiresApproval)
{
    private static readonly NullabilityInfoContext NullabilityContext = new();

    public CommandMetadata Resolve(Type commandType)
    {
        var spec = specs.Find(commandType);

        var description = spec?.Description
                          ?? Loc(Attr<BpmDescriptionAttribute>(commandType)?.Description);
        var policy = spec?.Policy
                     ?? Attr<BpmPolicyAttribute>(commandType)?.Policy
                     ?? defaultPolicy;
        var successCriteria = spec?.SuccessCriteria;

        var fields = commandType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetIndexParameters().Length == 0)
            .Select(p => ResolveField(commandType, p, spec?.Fields.GetValueOrDefault(p.Name)))
            .ToList();

        return new CommandMetadata(commandType.Name, commandType, description, policy, successCriteria, fields);
    }

    private FieldMetadata ResolveField(Type commandType, PropertyInfo property, AgentFieldData? spec)
    {
        var role = ClassifyRole(property, spec);

        var description = spec?.Description ?? Loc(Attr<BpmDescriptionAttribute>(property)?.Description);
        var sourceHint = spec?.SourceHint ?? Loc(Attr<BpmSourceAttribute>(property)?.SourceHint);
        var pattern = spec?.Pattern ?? Attr<BpmPatternAttribute>(property)?.Pattern;

        var rangeAttr = Attr<BpmRangeAttribute>(property);
        var min = spec?.Min ?? rangeAttr?.Min;
        var max = spec?.Max ?? rangeAttr?.Max;

        var required = spec?.Required
                       ?? (Attr<BpmRequiredAttribute>(property) is not null ? true : (bool?)null)
                       ?? !IsNullable(property);

        return new FieldMetadata(property.Name, property, role, description, sourceHint, required, pattern, min, max);
    }

    private FieldRole ClassifyRole(PropertyInfo property, AgentFieldData? spec)
    {
        // Fluent spec wins on conflict.
        if (spec?.Role is { } explicitRole)
            return explicitRole;

        if (Attr<BpmServerPopulatedAttribute>(property) is not null)
            return FieldRole.ServerPopulated;
        if (Attr<BpmDerivedAttribute>(property) is not null)
            return FieldRole.Derived;

        // Type-based convention: any property of a configured identity type is
        // server-populated, whatever it is named.
        if (identity.IdentityTypes.Contains(property.PropertyType))
            return FieldRole.ServerPopulated;

        // The process id is supplied by the adapter's route, never typed by the agent.
        if (property.Name.Equals("ProcessId", StringComparison.OrdinalIgnoreCase) &&
            (property.PropertyType == typeof(Guid) || property.PropertyType == typeof(Guid?)))
            return FieldRole.ServerPopulated;

        return FieldRole.Input;
    }

    // A single-language attribute value becomes the default variant of a LocalizedText.
    private static LocalizedText? Loc(string? text) => text is null ? null : text;

    private static TAttribute? Attr<TAttribute>(MemberInfo member) where TAttribute : Attribute =>
        member.GetCustomAttributes(typeof(TAttribute), false).Cast<TAttribute>().FirstOrDefault();

    private static bool IsNullable(PropertyInfo property)
    {
        if (Nullable.GetUnderlyingType(property.PropertyType) is not null)
            return true;
        if (property.PropertyType.IsValueType)
            return false;
        lock (NullabilityContext)
        {
            return NullabilityContext.Create(property).ReadState != NullabilityState.NotNull;
        }
    }
}
