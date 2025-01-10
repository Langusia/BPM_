using Core.BPM.Interfaces;
using Core.BPM.Nodes;
using Core.BPM.Application.Managers;

namespace Core.BPM;

public class BProcess(Type processType, INode rootNode)
{
    public readonly Type ProcessType = processType;
    public INode RootNode = rootNode;
    private List<INode>? _optionals;

    public List<List<INode>> AllPossibles { get; set; }
    public List<List<INode>> AllMandatoryPossibles => AllPossibles.Select(x => x.Where(node => node is not IOptional).ToList()).ToList();
    public List<INode> AllDistinctCommands { get; set; } = [];

    public void AddOptional(OptionalNode node, List<INode> prevSteps)
    {
        node.SetPrevSteps(prevSteps);
        _optionals ??= [];
        _optionals.Add(node);
    }

    public void AddDistinctCommand(INode node, List<INode>? prevSteps)
    {
        AllDistinctCommands ??= [];
        var cmd = AllDistinctCommands.FirstOrDefault(x => x.CommandType == node.CommandType);
        if (cmd is null)
            AllDistinctCommands.Add(node);
        else
            cmd.PrevSteps?.AddRange(prevSteps.ExceptBy(cmd.PrevSteps.Select(z => z.CommandType), x => x.CommandType).ToList());
    }


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

    public List<INode>? UnlockedPaths()
    {
        List<List<INode>> result = [];
        string[] _allEvents = ["Ad", "Zd", "Bd", "Fd"];

        //map events to Nodes
        foreach (var @event in _allEvents)
        {
            result = AllPossibles.Where(x => x.FirstOrDefault(z => z.ProducingEvents.Select(z => z.Name).Contains(@event)) is not null).ToList();
        }

        var rs = _allEvents.Select(x =>
                AllDistinctCommands.FirstOrDefault(z => z.ProducingEvents.Select(z => z.Name).ToList().Contains(x)))
            .Where(x => x is not null).ToList();

        var filtered = this.Filter(rs);


        return filtered;
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