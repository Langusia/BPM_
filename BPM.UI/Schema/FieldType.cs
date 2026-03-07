using System.Text.Json.Serialization;

namespace BPM.UI.Schema;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FieldType
{
    Text,
    Number,
    Decimal,
    Boolean,
    DateTime,
    Guid,
    File,
    Select
}
