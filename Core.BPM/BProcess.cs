using Core.BPM.Interfaces;

namespace Core.BPM;

public class BProcess(Type processType, INode rootNode)
{
    public readonly Type ProcessType = processType;
    public readonly INode RootNode = rootNode;

    public INode? FindLastValidNode(List<string> progressedPath, bool skipOptionals = false)
    {
        if (progressedPath.Count == 0)
            return null;

        var currentNode = RootNode;

        foreach (var eventName in progressedPath)
        {
            if (currentNode.ProducingEvents.Any(e => e.Name == eventName))
            {
                // Found a root match, skipping...
                continue;
            }

            currentNode = currentNode.FindNextNode(eventName);
            if (currentNode == null)
                break; // Stop traversal if no matching node is found for the event
        }

        return currentNode; // Return the last node reached
    }

    public List<INode> GetNodes(Type commandType)
    {
        var matchingNodes = new List<INode>();
        FindNodes(RootNode, commandType, matchingNodes);
        return matchingNodes;
    }

    private void FindNodes(INode currentNode, Type commandType, List<INode> matchingNodes)
    {
        // Check if the current node matches the command type
        if (currentNode.CommandType == commandType)
        {
            matchingNodes.Add(currentNode);
        }

        // Recursively check the next steps
        if (currentNode.NextSteps != null)
        {
            foreach (var nextNode in currentNode.NextSteps)
            {
                FindNodes(nextNode, commandType, matchingNodes);
            }
        }
    }
}