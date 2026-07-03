using System;
using System.Collections.Generic;
using System.Reflection;
using BPM.Contracts;

namespace BPM.Core.Application.Metadata;

/// <summary>
/// Fully merged agent-grade metadata for a command: fluent spec over attributes
/// over reflection defaults. This — together with the schema projection model —
/// is the shared SPI all adapters (MCP today, UI 2.0 later) consume.
/// </summary>
public sealed record CommandMetadata(
    string Name,
    Type CommandType,
    string? Description,
    ExecutionPolicy Policy,
    string? SuccessCriteria,
    IReadOnlyList<FieldMetadata> Fields);

/// <summary>Merged metadata for a single command property.</summary>
public sealed record FieldMetadata(
    string Name,
    PropertyInfo Property,
    FieldRole Role,
    string? Description,
    string? SourceHint,
    bool Required,
    string? Pattern,
    double? Minimum,
    double? Maximum);
