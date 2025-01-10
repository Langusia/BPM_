using Core.BPM.Application.Managers;
using Core.BPM.Interfaces;

namespace Core.BPM.Nodes;

public class AnyTimeNode(Type commandType, Type processType) : NodeBase(commandType, processType), IMulti
{
    public override bool ValidatePlacement(BProcess process, List<string> savedEvents, INode? currentNode)
    {
        return PlacementPreconditionMarked(savedEvents);
    }

    public List<List<INode>> Filter(List<List<INode>> filterFrom, List<INode> storedNodes)
    {
        throw new NotImplementedException();
    }
}