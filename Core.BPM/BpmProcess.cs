using Core.BPM.Interfaces;

namespace Core.BPM;

public class BpmProcess<TProcess>(INode<TProcess> rootNode)
    : BpmProcess(typeof(TProcess), rootNode), IProcess<TProcess> where TProcess : IAggregate
{
    public INode<TProcess> RootNode { get; } = rootNode;
    protected override INode<TProcess> GetRootNode() => rootNode;

    public INode<TProcess>? MoveTo<TCommand>() => rootNode.MoveTo(typeof(TCommand));

    public List<INode<TProcess>> GetConditionValidGraphNodes(TProcess aggregate)
    {
        var result = new List<INode<TProcess>>();
        rootNode.GetValidNodes(aggregate, result);
        return result;
    }
}

public class BpmProcess(Type processType, INode rootNode)
{
    public readonly Type ProcessType = processType;

    protected virtual INode GetRootNode() => rootNode;
}

