using Core.BPM.Configuration;
using Core.BPM.Evaluators.Factory;
using Core.BPM.Interfaces;

namespace Core.BPM.Nodes;

public class GuestProcessNode(Type aggregateType, Type processType, INodeEvaluatorFactory nodeEvaluatorFactory)
    : NodeBase(typeof(GuestProcessNode), processType, nodeEvaluatorFactory), INode
{
    public Type AggregateType { get; init; } = aggregateType;
}