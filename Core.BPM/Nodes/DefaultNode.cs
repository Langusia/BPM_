using Core.BPM.Application.Managers;
using Core.BPM.Interfaces;

namespace Core.BPM.Nodes;

public class Node(Type commandType, Type processType) : NodeBase(commandType, processType)
{
    public override bool ValidatePlacement(List<string> savedEvents, INode? currentNode)
    {
        bool alreadyExists = savedEvents.Any(tuple =>
            GetCommandProducer(CommandType).EventTypes.Select(x => x.Name).Contains(tuple));
        if (alreadyExists)
            return false;

        if (currentNode != null && !currentNode.NextSteps?.Contains(this) == true)
        {
            // If the current node does not allow this AnyTimeNode to be added, return false.
            return true; // Invalid if currentNode does not directly allow it.
        }

        return true;
    }
}