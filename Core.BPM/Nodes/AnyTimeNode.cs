using Core.BPM.Application.Managers;
using Core.BPM.Evaluators;
using Core.BPM.Interfaces;

namespace Core.BPM.Nodes;

public class AnyTimeNode(Type commandType, Type processType) : NodeBase(commandType, processType), INode, IMulti
{
    public INodeStateEvaluator GetEvaluator() => new AnyTimeNodeStateEvaluator(this);
}