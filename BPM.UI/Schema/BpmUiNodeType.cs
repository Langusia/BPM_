using System.Text.Json.Serialization;

namespace BPM.UI.Schema;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BpmUiNodeType
{
    Standard,
    Informational,
    Optional,
    AnyTime,
    JumpTo,
    Group
}
