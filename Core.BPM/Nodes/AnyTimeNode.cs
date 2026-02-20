using System;
using Core.BPM.Nodes.Evaluation;

namespace Core.BPM.Nodes;

public class AnyTimeNode(Type commandType, Type processType, INodeEvaluatorFactory nodeEvaluatorFactory) : NodeBase(commandType, processType, nodeEvaluatorFactory), INode
{
    public INodeStateEvaluator GetEvaluator() => new AnyTimeNodeStateEvaluator(this);
}
