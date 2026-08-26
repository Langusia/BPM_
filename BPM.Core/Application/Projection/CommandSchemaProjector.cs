using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BPM.Contracts;
using BPM.Core.Application.Catalog;
using BPM.Core.Application.Metadata;

namespace BPM.Core.Application.Projection;

/// <summary>
/// Projects a command type into the neutral <see cref="CommandSchemaModel"/>:
/// three-bucket field classification, C# enums to enum value lists, validation
/// metadata to schema constraints. Serves both the generic dispatch tools and
/// (later) dynamically registered per-command tools.
/// </summary>
public sealed class CommandSchemaProjector(CommandMetadataResolver metadataResolver)
{
    private const int MaxDepth = 8;

    public CommandSchemaModel Project(CatalogCommandDescriptor command)
    {
        var metadata = metadataResolver.Resolve(command.CommandType);

        var fields = metadata.Fields
            .Where(f => f.Role == FieldRole.Input)
            .Select(f => ProjectField(f, depth: 0))
            .ToList();

        return new CommandSchemaModel(
            command.Name,
            command.AggregateTypeName,
            metadata.Description,
            metadata.Policy,
            metadata.SuccessCriteria,
            command.IsInitial,
            fields);
    }

    /// <summary>Merged metadata for a command, including non-input buckets (for logging/diagnostics).</summary>
    public CommandMetadata ResolveMetadata(Type commandType) => metadataResolver.Resolve(commandType);

    private SchemaFieldModel ProjectField(FieldMetadata field, int depth)
    {
        var (kind, format, enumValues, items, properties) =
            MapType(field.Property.PropertyType, depth);

        var description = Combine(field.Description, field.SourceHint);

        return new SchemaFieldModel(
            field.Name,
            kind,
            field.Required,
            description,
            field.SourceHint,
            enumValues,
            field.Pattern,
            field.Minimum,
            field.Maximum,
            format,
            items,
            properties);
    }

    private (SchemaFieldKind Kind, string? Format, IReadOnlyList<string>? EnumValues, SchemaFieldModel? Items, IReadOnlyList<SchemaFieldModel>? Properties)
        MapType(Type type, int depth)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type.IsEnum)
            return (SchemaFieldKind.Enum, null, Enum.GetNames(type), null, null);

        if (type == typeof(string))
            return (SchemaFieldKind.String, null, null, null, null);
        if (type == typeof(Guid))
            return (SchemaFieldKind.String, "uuid", null, null, null);
        if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
            return (SchemaFieldKind.String, "date-time", null, null, null);
        if (type == typeof(DateOnly))
            return (SchemaFieldKind.String, "date", null, null, null);
        if (type == typeof(TimeOnly) || type == typeof(TimeSpan))
            return (SchemaFieldKind.String, "time", null, null, null);
        if (type == typeof(bool))
            return (SchemaFieldKind.Boolean, null, null, null, null);
        if (type == typeof(byte) || type == typeof(sbyte) || type == typeof(short) || type == typeof(ushort) ||
            type == typeof(int) || type == typeof(uint) || type == typeof(long) || type == typeof(ulong))
            return (SchemaFieldKind.Integer, null, null, null, null);
        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            return (SchemaFieldKind.Number, null, null, null, null);

        if (depth >= MaxDepth)
            return (SchemaFieldKind.Object, null, null, null, null);

        var elementType = GetEnumerableElementType(type);
        if (elementType is not null)
        {
            var (elemKind, elemFormat, elemEnums, elemItems, elemProps) = MapType(elementType, depth + 1);
            var itemModel = new SchemaFieldModel(
                "item", elemKind, true, null, null, elemEnums, null, null, null, elemFormat, elemItems, elemProps);
            return (SchemaFieldKind.Array, null, null, itemModel, null);
        }

        // Nested complex object: project its public properties recursively.
        var nested = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetIndexParameters().Length == 0)
            .Select(p =>
            {
                var (k, f, e, i, props) = MapType(p.PropertyType, depth + 1);
                var desc = p.GetCustomAttributes(typeof(BpmDescriptionAttribute), false)
                    .Cast<BpmDescriptionAttribute>().FirstOrDefault()?.Description;
                var required = Nullable.GetUnderlyingType(p.PropertyType) is null && p.PropertyType.IsValueType;
                return new SchemaFieldModel(p.Name, k, required, desc, null, e, null, null, null, f, i, props);
            })
            .ToList();

        return (SchemaFieldKind.Object, null, null, null, nested);
    }

    private static Type? GetEnumerableElementType(Type type)
    {
        if (type == typeof(string))
            return null;
        if (type.IsArray)
            return type.GetElementType();
        if (!typeof(IEnumerable).IsAssignableFrom(type))
            return null;
        return type.GetInterfaces()
            .Concat(type.IsInterface ? [type] : Array.Empty<Type>())
            .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            .Select(i => i.GetGenericArguments()[0])
            .FirstOrDefault();
    }

    private static string? Combine(string? description, string? sourceHint)
    {
        if (description is null)
            return sourceHint is null ? null : $"Source: {sourceHint}";
        return sourceHint is null ? description : $"{description} (Source: {sourceHint})";
    }
}
