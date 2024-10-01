using Core.BPM.Interfaces;

namespace Core.BPM;

public class BProcess(Type processType, INode rootNode)
{
    public readonly Type ProcessType = processType;
    public readonly INode RootNode = rootNode;

    public INode? FindLastValidNode(List<string>? progressedPath)
    {
        if (progressedPath == null || progressedPath.Count == 0)
            return RootNode;

        var currentNode = RootNode;

        foreach (var eventName in progressedPath)
        {
            if (currentNode.ProducingEvents.Any(e => e.Name == eventName))
            {
                // Found a match, so we don't need to move yet
                continue;
            }

            currentNode = currentNode.FindNextNode(eventName);
            if (currentNode == null)
                break; // Stop traversal if no matching node is found for the event
        }

        return currentNode; // Return the last node reached
    }
}