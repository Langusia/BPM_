using Core.BPM.Application.Managers;
using Core.BPM.Evaluators;
using Core.BPM.Interfaces;

namespace Core.BPM.Nodes;

public class OptionalNode(Type commandType, Type processType) : NodeBase(commandType, processType), INode, IOptional
{
    public INodeStateEvaluator GetEvaluator()
    {
        throw new NotImplementedException();
    }
}