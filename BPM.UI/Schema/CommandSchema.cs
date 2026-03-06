namespace BPM.UI.Schema;

public class CommandSchema
{
    public string CommandName { get; set; } = default!;
    public string NodeId { get; set; } = default!;
    public string Endpoint { get; set; } = default!;
    public string HttpMethod { get; set; } = default!;
    public BpmUiNodeType NodeType { get; set; }
    public List<FieldSchema> Fields { get; set; } = [];
}
