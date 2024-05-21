using Core.BPM.Interfaces;

namespace Core.BPM;

public class BpmResult<T> where T : Aggregate
{
    public T Aggregate { get; set; }
    public INode CurrentNode { get; set; }
    public List<string>? NextNodes { get; set; }
}