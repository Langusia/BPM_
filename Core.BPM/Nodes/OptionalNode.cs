using Core.BPM.Application.Managers;
using Core.BPM.Interfaces;

namespace Core.BPM.Nodes;

public class OptionalNode(Type commandType, Type processType) : NodeBase(commandType, processType)
{
    public override bool Validate(List<MutableTuple<string, INode?>> events, INode currentNode)
    {
        if (currentNode.NextSteps is not null)
            if (currentNode.NextSteps.Any(x => x == this))
                return true;

        return false;
    }
}