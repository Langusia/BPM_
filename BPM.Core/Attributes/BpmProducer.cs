using System;

namespace BPM.Core.Attributes;

public class BpmProducer : Attribute
{
    public BpmProducer(params Type[] eventTypes)
    {
        EventTypes = eventTypes;
    }

    public Type[] EventTypes { get; }
}