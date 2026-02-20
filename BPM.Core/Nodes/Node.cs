using System;
using BPM.Core.Nodes.Evaluation;

namespace BPM.Core.Nodes;

public class Node(Type commandType, Type processType, INodeEvaluatorFactory nodeEvaluatorFactory) : NodeBase(commandType, processType, nodeEvaluatorFactory)
{
    public override INodeStateEvaluator GetEvaluator() => new NodeStateEvaluator(this);
}
