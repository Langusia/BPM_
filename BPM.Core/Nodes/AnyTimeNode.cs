using System;
using BPM.Core.Nodes.Evaluation;

namespace BPM.Core.Nodes;

public class AnyTimeNode(Type commandType, Type processType, INodeEvaluatorFactory nodeEvaluatorFactory) : NodeBase(commandType, processType, nodeEvaluatorFactory), INode
{
    public INodeStateEvaluator GetEvaluator() => new AnyTimeNodeStateEvaluator(this);
}
