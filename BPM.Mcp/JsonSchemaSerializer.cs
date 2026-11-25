using System.Text.Json;
using System.Text.Json.Nodes;
using BPM.Core.Application.Projection;

namespace BPM.Mcp;

/// <summary>
/// Serializes the engine's neutral <see cref="CommandSchemaModel"/> projection
/// into a JSON Schema document for agents. The only place JSON-Schema
/// vocabulary exists; the engine model stays transport-neutral.
/// </summary>
internal static class JsonSchemaSerializer
{
    public static JsonObject ToJsonSchema(CommandSchemaModel model)
    {
        var properties = new JsonObject();
        var required = new JsonArray();

        foreach (var field in model.Fields)
        {
            var name = CamelCase(field.Name);
            properties[name] = FieldSchema(field);
            if (field.Required)
                required.Add(name);
        }

        var schema = new JsonObject
        {
            ["type"] = "object",
            ["title"] = model.CommandName,
            ["properties"] = properties,
            ["additionalProperties"] = false
        };
        if (model.Description is not null)
            schema["description"] = model.Description;
        if (required.Count > 0)
            schema["required"] = required;

        return schema;
    }

    private static JsonObject FieldSchema(SchemaFieldModel field)
    {
        var node = new JsonObject();

        switch (field.Kind)
        {
            case SchemaFieldKind.String:
                node["type"] = "string";
                break;
            case SchemaFieldKind.Integer:
                node["type"] = "integer";
                break;
            case SchemaFieldKind.Number:
                node["type"] = "number";
                break;
            case SchemaFieldKind.Boolean:
                node["type"] = "boolean";
                break;
            case SchemaFieldKind.Enum:
                node["type"] = "string";
                if (field.EnumValues is not null)
                    node["enum"] = new JsonArray(field.EnumValues.Select(v => (JsonNode)v).ToArray());
                break;
            case SchemaFieldKind.Array:
                node["type"] = "array";
                if (field.Items is not null)
                    node["items"] = FieldSchema(field.Items);
                break;
            case SchemaFieldKind.Object:
                node["type"] = "object";
                if (field.Properties is not null)
                {
                    var nested = new JsonObject();
                    var nestedRequired = new JsonArray();
                    foreach (var property in field.Properties)
                    {
                        var name = CamelCase(property.Name);
                        nested[name] = FieldSchema(property);
                        if (property.Required)
                            nestedRequired.Add(name);
                    }
                    node["properties"] = nested;
                    if (nestedRequired.Count > 0)
                        node["required"] = nestedRequired;
                }
                break;
        }

        if (field.Description is not null)
            node["description"] = field.Description;
        if (field.Format is not null)
            node["format"] = field.Format;
        if (field.Pattern is not null)
            node["pattern"] = field.Pattern;
        if (field.Minimum is { } min)
            node["minimum"] = min;
        if (field.Maximum is { } max)
            node["maximum"] = max;

        return node;
    }

    private static string CamelCase(string name) => JsonNamingPolicy.CamelCase.ConvertName(name);
}
