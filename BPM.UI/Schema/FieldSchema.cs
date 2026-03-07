namespace BPM.UI.Schema;

public class FieldSchema
{
    public string Name { get; set; } = default!;
    public string Label { get; set; } = default!;
    public FieldType Type { get; set; }
    public bool IsHidden { get; set; }
    public bool IsRequired { get; set; }
    public string? Regex { get; set; }
    public double? Min { get; set; }
    public double? Max { get; set; }
    public string? Placeholder { get; set; }
    public List<string>? Options { get; set; }
}
