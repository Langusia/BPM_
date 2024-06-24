using Core.BPM.Interfaces;

namespace Core.BPM;

public class BProcess(Type processType, INode rootNode)
{
    public readonly Type ProcessType = processType;
    public readonly INode RootNode = rootNode;
}

public class BProcessAggregateState(Type processType, INode rootNode) : BProcess(processType, rootNode)
{
    public Aggregate Aggregate { get; set; }
    
    public INode CurrentStep { get; set; }
}