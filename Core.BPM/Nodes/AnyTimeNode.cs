using Core.BPM.Application.Managers;
using Core.BPM.Evaluators;
using Core.BPM.Evaluators.Factory;
using Core.BPM.Interfaces;

namespace Core.BPM.Nodes;

public class AnyTimeNode(Type commandType, Type processType, INodeEvaluatorFactory nodeEvaluatorFactory) : NodeBase(commandType, processType, nodeEvaluatorFactory), INode
{
    public INodeStateEvaluator GetEvaluator() => new AnyTimeNodeStateEvaluator(this);
}