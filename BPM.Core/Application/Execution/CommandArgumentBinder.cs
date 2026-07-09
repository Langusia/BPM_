using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using BPM.Contracts;
using BPM.Core.Application.Metadata;

namespace BPM.Core.Application.Execution;

/// <summary>
/// Binds agent-supplied JSON arguments to a command instance using the merged
/// field metadata. Only <see cref="FieldRole.Input"/> fields bind from JSON;
/// agent-supplied values for server-populated or derived fields are discarded
/// unconditionally (and reported so the caller can log them). The process id
/// comes from the route argument, never from the JSON body.
/// </summary>
public sealed class CommandArgumentBinder
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    public sealed record BindOutcome(
        object? Command,
        IReadOnlyList<string> DiscardedFields,
        AgentError? Error);

    public BindOutcome Bind(CommandMetadata metadata, string? argsJson, Guid? processId)
    {
        JsonObject args;
        if (string.IsNullOrWhiteSpace(argsJson))
        {
            args = new JsonObject();
        }
        else
        {
            try
            {
                args = JsonNode.Parse(argsJson) as JsonObject
                       ?? throw new JsonException("arguments must be a JSON object");
            }
            catch (JsonException ex)
            {
                return new BindOutcome(null, [], new AgentError(
                    AgentErrorCodes.InvalidArguments,
                    $"argsJson is not a valid JSON object: {ex.Message}",
                    "Pass the command arguments as a single JSON object, e.g. {\"amount\": 5000}."));
            }
        }

        var fieldsByName = metadata.Fields.ToDictionary(f => f.Name, StringComparer.OrdinalIgnoreCase);

        // Discard agent-supplied values for anything that is not an input field.
        var discarded = new List<string>();
        foreach (var key in args.Select(kv => kv.Key).ToList())
        {
            if (fieldsByName.TryGetValue(key, out var field) && field.Role != FieldRole.Input)
            {
                args.Remove(key);
                discarded.Add(field.Name);
            }
        }

        // Validate required inputs before deserializing, so the agent gets a
        // complete list instead of a serializer error.
        var missing = metadata.Fields
            .Where(f => f.Role == FieldRole.Input && f.Required)
            .Where(f => !args.Any(kv =>
                string.Equals(kv.Key, f.Name, StringComparison.OrdinalIgnoreCase) &&
                kv.Value is not null))
            .Select(f => f.Name)
            .ToList();
        if (missing.Count > 0)
        {
            return new BindOutcome(null, discarded, new AgentError(
                AgentErrorCodes.InvalidArguments,
                $"Missing required argument(s): {string.Join(", ", missing)}.",
                $"Call bpm_get_command_schema(\"{metadata.Name}\") for the full schema.",
                new { missingFields = missing }));
        }

        // The route argument is the only source of the process id.
        var processIdField = metadata.Fields.FirstOrDefault(f =>
            f.Name.Equals("ProcessId", StringComparison.OrdinalIgnoreCase));
        if (processIdField is not null && processId is { } id)
            args[processIdField.Name] = id.ToString();

        try
        {
            var command = args.Deserialize(metadata.CommandType, SerializerOptions);
            return new BindOutcome(command, discarded, null);
        }
        catch (JsonException ex)
        {
            return new BindOutcome(null, discarded, new AgentError(
                AgentErrorCodes.InvalidArguments,
                $"Arguments do not match the schema of '{metadata.Name}': {ex.Message}",
                $"Call bpm_get_command_schema(\"{metadata.Name}\") to see expected fields and types."));
        }
    }
}
