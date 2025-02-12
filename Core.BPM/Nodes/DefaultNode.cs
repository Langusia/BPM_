using Core.BPM.Application.Managers;
using Core.BPM.Evaluators;
using Core.BPM.Interfaces;

namespace Core.BPM.Nodes;

public class Node(Type commandType, Type processType) : NodeBase(commandType, processType), INode
{
    public INodeStateEvaluator GetEvaluator() => new NodeStateEvaluator();
}