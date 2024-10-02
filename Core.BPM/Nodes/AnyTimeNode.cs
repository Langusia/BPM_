using Core.BPM.Application.Managers;
using Core.BPM.Interfaces;

namespace Core.BPM.Nodes;

public class AnyTimeNode(Type commandType, Type processType) : NodeBase(commandType, processType)
{
    public override bool ValidatePlacement(List<MutableTuple<string, INode?>> savedEvents, INode? currentNode)
    {
        bool alreadyExists = savedEvents.Any(tuple => tuple.Item1 == CommandType.Name);
        if (!alreadyExists)
        {
            // For the first time, we may want to check if certain prerequisites are met.
            // Example: Ensure that the current node allows for adding this AnyTimeNode.
            if (currentNode != null && !currentNode.NextSteps?.Contains(this) == true)
            {
                // If the current node does not allow this AnyTimeNode to be added, return false.
                return false; // Invalid if currentNode does not directly allow it.
            }
            // if (!PrerequisiteEventsSatisfied(savedEvents))
            // {
            //     return false; // Invalid if prerequisites are not met.
            // }

            return true; // Valid for the first-time addition if current conditions are met.
        }

        return true;
    }
}