using Core.BPM.Interfaces;

namespace Core.BPM;

public class BpmResult<T>(T aggregate) : BpmResult
    where T : Aggregate
{
    public T Aggregate { get; init; } = aggregate;
}

public class BpmResult
{
    public Guid AggregateId { get; init; }
    public INode CurrentNode { get; init; }
    public List<string>? NextNodes { get; init; }
}