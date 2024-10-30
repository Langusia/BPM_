using Core.BPM.Application.Managers;
using Core.BPM.Interfaces;

namespace Core.BPM.Nodes;

public class OptionalNode(Type commandType, Type processType) : NodeBase(commandType, processType)
{
    public override bool ValidatePlacement(List<string> savedEvents, INode? currentNode)
    {
        bool alreadyExists = savedEvents.Any(tuple =>
            GetCommandProducer(CommandType).EventTypes.Select(x => x.Name).Contains(tuple));

        if (alreadyExists)
            return false;

        return currentNode != null && !currentNode.NextSteps?.Contains(this) == true;
    }
}