using System;

// Kept in the original BPM.Core.Attributes namespace: this attribute moved from
// BPM.Core so command assemblies can compile against BPM.Contracts alone, while
// existing source that references BPM.Core.Attributes keeps compiling unchanged.
namespace BPM.Core.Attributes;

public class BpmProducer : Attribute
{
    public BpmProducer(params Type[] eventTypes)
    {
        EventTypes = eventTypes;
    }

    public Type[] EventTypes { get; }
}
