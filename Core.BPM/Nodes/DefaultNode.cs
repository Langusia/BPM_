using System;
using Core.BPM.Evaluators;
using Core.BPM.Evaluators.Factory;

namespace Core.BPM.Nodes;

public class Node(Type commandType, Type processType, INodeEvaluatorFactory nodeEvaluatorFactory) : NodeBase(commandType, processType, nodeEvaluatorFactory)
{
    public override INodeStateEvaluator GetEvaluator() => new NodeStateEvaluator(this);
}