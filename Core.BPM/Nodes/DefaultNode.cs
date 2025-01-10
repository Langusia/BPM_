using Core.BPM.Application.Managers;
using Core.BPM.Interfaces;

namespace Core.BPM.Nodes;

public class Node(Type commandType, Type processType) : NodeBase(commandType, processType)
{
    public override bool ValidatePlacement(BProcess process, List<string> savedEvents, INode? currentNode)
    {
        var preconditionsMet = PlacementPreconditionMarked(savedEvents);
        if (!preconditionsMet)
            return false;

        var alreadyExists = savedEvents.Any(tuple => GetCommandProducer(CommandType).EventTypes.Select(x => x.Name).Contains(tuple));
        return !alreadyExists;
    }

    public List<List<INode>> Filter(List<List<INode>> filterFrom, List<INode> storedNodes)
    {
        throw new NotImplementedException();
    }
}