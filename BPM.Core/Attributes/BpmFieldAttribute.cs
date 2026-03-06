using System;

namespace BPM.Core.Attributes;

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property)]
public class BpmFieldAttribute : Attribute
{
    public string? Label { get; set; }
    public string? Regex { get; set; }
    public double? Min { get; set; }
    public double? Max { get; set; }
    public string? Placeholder { get; set; }
}
