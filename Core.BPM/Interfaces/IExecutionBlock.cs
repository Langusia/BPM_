namespace Core.BPM.Interfaces;

public interface IExecutionBlock
{
}

public interface SequentialBlock : IExecutionBlock
{
    public List<INode> Nodes { get; set; }
}

public interface NonSequentialGroupBlock : IExecutionBlock
{
    public List<INode> Nodes { get; set; }
}