namespace Core.BPM.MediatR.Attributes;

public class BpmProducer : Attribute
{
    public BpmProducer(params Type[] eventTypes)
    {
        EventTypes = eventTypes;
    }

    public Type[] EventTypes { get; }
}