using Core.BPM.Application.Managers;
using Core.BPM.Interfaces;

namespace Core.BPM.Nodes;

public class OptionalNode(Type commandType, Type processType) : NodeBase(commandType, processType)
{
    public override bool ValidatePlacement(List<MutableTuple<string, INode?>> savedEvents, INode? currentNode)
    {
        var alreadyExists = savedEvents.Any(tuple => tuple.Item1 == CommandType.Name);
        if (alreadyExists)
            return false;

        return currentNode != null && !currentNode.NextSteps?.Contains(this) == true;
    }
}