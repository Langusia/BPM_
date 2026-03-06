using System.Text.Json.Serialization;

namespace BPM.UI.Endpoints;

public class GraphResponse
{
    [JsonPropertyName("aggregateName")]
    public string AggregateName { get; set; } = default!;

    [JsonPropertyName("processId")]
    public Guid? ProcessId { get; set; }

    [JsonPropertyName("nodes")]
    public List<GraphNode> Nodes { get; set; } = [];

    [JsonPropertyName("edges")]
    public List<GraphEdge> Edges { get; set; } = [];
}

public class GraphNode
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    [JsonPropertyName("commandName")]
    public string CommandName { get; set; } = default!;

    [JsonPropertyName("label")]
    public string Label { get; set; } = default!;

    [JsonPropertyName("status")]
    public string Status { get; set; } = default!;

    [JsonPropertyName("nodeType")]
    public string NodeType { get; set; } = default!;

    [JsonPropertyName("endpoint")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Endpoint { get; set; }

    [JsonPropertyName("httpMethod")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? HttpMethod { get; set; }

    [JsonPropertyName("members")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Members { get; set; }

    [JsonPropertyName("subGraphEndpoint")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SubGraphEndpoint { get; set; }
}

public class GraphEdge
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = default!;

    [JsonPropertyName("source")]
    public string Source { get; set; } = default!;

    [JsonPropertyName("target")]
    public string Target { get; set; } = default!;

    [JsonPropertyName("conditionMet")]
    public bool? ConditionMet { get; set; }
}

public class ExecuteResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("processId")]
    public Guid? ProcessId { get; set; }

    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Error { get; set; }

    [JsonPropertyName("availableStepIds")]
    public List<string> AvailableStepIds { get; set; } = [];
}
