using System.Reflection;
using BPM.Core.Attributes;
using BPM.Core.Configuration;
using BPM.Core.Nodes;
using BPM.UI.Configuration;
using Microsoft.AspNetCore.Http;

namespace BPM.UI.Schema;

public static class CommandSchemaBuilder
{
    public static void PopulateRegistry(BpmSchemaRegistry registry, BpmUiConfiguration uiConfig)
    {
        var processes = BProcessGraphConfiguration.GetAllProcesses();
        if (processes is null) return;

        foreach (var process in processes)
        {
            var allNodes = process.RootNode.GetAllNodes();
            foreach (var node in allNodes)
            {
                if (node.CommandType == typeof(GroupNode) ||
                    node.CommandType == typeof(ConditionalNode) ||
                    node.CommandType == typeof(GuestProcessNode))
                    continue;

                var commandName = node.CommandType.Name;
                var nodeId = $"{commandName}_{node.NodeLevel}";
                var nodeType = ResolveNodeType(node);
                var httpMethod = nodeType == BpmUiNodeType.Informational ? "GET" : "POST";
                var endpoint = uiConfig.ResolveEndpoint(commandName, process.ProcessType);

                var schema = new CommandSchema
                {
                    CommandName = commandName,
                    NodeId = nodeId,
                    Endpoint = endpoint,
                    HttpMethod = httpMethod,
                    NodeType = nodeType,
                    Fields = BuildFields(node.CommandType)
                };

                registry.Register(schema);
            }
        }
    }

    private static BpmUiNodeType ResolveNodeType(INode node)
    {
        return node switch
        {
            OptionalNode => BpmUiNodeType.Optional,
            AnyTimeNode => BpmUiNodeType.AnyTime,
            _ => BpmUiNodeType.Standard
        };
    }

    public static List<FieldSchema> BuildFields(Type commandType)
    {
        var fields = new List<FieldSchema>();
        var properties = commandType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var constructorParams = commandType.GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault()?.GetParameters();

        foreach (var prop in properties)
        {
            var field = BuildFieldFromMember(prop.Name, prop.PropertyType,
                prop.GetCustomAttribute<BpmFieldAttribute>(),
                prop.GetCustomAttribute<BpmInternalAttribute>(),
                IsNullable(prop.PropertyType, prop));
            fields.Add(field);
        }

        if (constructorParams is not null)
        {
            foreach (var param in constructorParams)
            {
                if (fields.Any(f => string.Equals(f.Name, param.Name, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var field = BuildFieldFromMember(param.Name!, param.ParameterType,
                    param.GetCustomAttribute<BpmFieldAttribute>(),
                    param.GetCustomAttribute<BpmInternalAttribute>(),
                    IsNullableParam(param));
                fields.Add(field);
            }
        }

        return fields;
    }

    private static FieldSchema BuildFieldFromMember(string name, Type type,
        BpmFieldAttribute? fieldAttr, BpmInternalAttribute? internalAttr, bool isNullable)
    {
        var fieldType = MapClrType(type);
        var schema = new FieldSchema
        {
            Name = name,
            Label = fieldAttr?.Label ?? HumanizeName(name),
            Type = fieldType,
            IsHidden = internalAttr is not null,
            IsRequired = !isNullable,
            Regex = fieldAttr?.Regex,
            Min = fieldAttr?.Min,
            Max = fieldAttr?.Max,
            Placeholder = fieldAttr?.Placeholder
        };

        if (fieldType == FieldType.Select)
        {
            var enumType = Nullable.GetUnderlyingType(type) ?? type;
            schema.Options = Enum.GetNames(enumType).ToList();
        }

        return schema;
    }

    private static FieldType MapClrType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying == typeof(string)) return FieldType.Text;
        if (underlying == typeof(int) || underlying == typeof(long)) return FieldType.Number;
        if (underlying == typeof(decimal) || underlying == typeof(double) || underlying == typeof(float)) return FieldType.Decimal;
        if (underlying == typeof(bool)) return FieldType.Boolean;
        if (underlying == typeof(DateTime) || underlying == typeof(DateTimeOffset)) return FieldType.DateTime;
        if (underlying == typeof(Guid)) return FieldType.Guid;
        if (typeof(IFormFile).IsAssignableFrom(underlying)) return FieldType.File;
        if (underlying.IsEnum) return FieldType.Select;

        return FieldType.Text;
    }

    private static bool IsNullable(Type type, PropertyInfo prop)
    {
        if (Nullable.GetUnderlyingType(type) is not null) return true;
        if (!type.IsValueType)
        {
            var context = new NullabilityInfoContext();
            var info = context.Create(prop);
            return info.WriteState == NullabilityState.Nullable || info.ReadState == NullabilityState.Nullable;
        }
        return false;
    }

    private static bool IsNullableParam(ParameterInfo param)
    {
        if (Nullable.GetUnderlyingType(param.ParameterType) is not null) return true;
        if (!param.ParameterType.IsValueType)
        {
            var context = new NullabilityInfoContext();
            var info = context.Create(param);
            return info.WriteState == NullabilityState.Nullable || info.ReadState == NullabilityState.Nullable;
        }
        return false;
    }

    private static string HumanizeName(string name)
    {
        var result = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(name[i - 1]))
                result.Append(' ');
            result.Append(i == 0 ? char.ToUpper(c) : c);
        }
        return result.ToString();
    }
}
