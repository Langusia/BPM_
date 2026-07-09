using System.Collections.Generic;
using BPM.Contracts;

namespace BPM.Core.Application.Projection;

/// <summary>
/// Transport-neutral, agent-facing schema for a command — a projection of the
/// command record, never the raw record. Contains only
/// <see cref="FieldRole.Input"/> fields; server-populated and derived fields
/// never appear. Adapters serialize this model to their wire format
/// (JSON Schema for MCP, form metadata for UI 2.0). Treat this shape as a
/// versioned public API.
/// </summary>
public sealed record CommandSchemaModel(
    string CommandName,
    string AggregateTypeName,
    string? Description,
    ExecutionPolicy Policy,
    string? SuccessCriteria,
    bool IsInitial,
    IReadOnlyList<SchemaFieldModel> Fields);

/// <summary>One agent-input field of a projected command schema.</summary>
public sealed record SchemaFieldModel(
    string Name,
    SchemaFieldKind Kind,
    bool Required,
    string? Description,
    string? SourceHint,
    IReadOnlyList<string>? EnumValues,
    string? Pattern,
    double? Minimum,
    double? Maximum,
    string? Format,
    SchemaFieldModel? Items,
    IReadOnlyList<SchemaFieldModel>? Properties);

/// <summary>Neutral field kind; adapters map this to their own type system.</summary>
public enum SchemaFieldKind
{
    String,
    Integer,
    Number,
    Boolean,
    Enum,
    Object,
    Array
}
