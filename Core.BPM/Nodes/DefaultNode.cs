using Core.BPM.Evaluators;

namespace Core.BPM.Nodes;

public class Node(Type commandType, Type processType) : NodeBase(commandType, processType)
{
    public override INodeStateEvaluator GetEvaluator() => new NodeStateEvaluator(this);
}