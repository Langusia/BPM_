using Core.BPM.Interfaces;

namespace Core.BPM.Nodes;

public class OptionalNode(Type commandType, Type processType) : NodeBase(commandType, processType), IOptional
{
    public override bool ValidatePlacement(BProcess process, List<string> savedEvents, INode? currentNode)
    {
        return ValidatePrecondition(savedEvents);
    }
}