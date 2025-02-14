using Core.BPM.AggregateConditions;
using Core.BPM.Evaluators;
using Core.BPM.Interfaces;

namespace Core.BPM.Nodes;

public class ConditionalNode(Type processType, IAggregateCondition aggregateCondition) : NodeBase(typeof(ConditionalNode), processType)
{
    private IAggregateCondition _aggregateCondition = aggregateCondition;

    public INode IfRootNode { get; set; }
    public INode? ElseRootNode { get; set; }

    public override INodeStateEvaluator GetEvaluator() => new ConditionalNodeStateEvaluator(this);
    public override bool ContainsEvent(object @event) => IfRootNode.GetAllNodes().Union(ElseRootNode?.GetAllNodes() ?? Enumerable.Empty<INode>()).Any(x => x.ContainsEvent(@event));
}