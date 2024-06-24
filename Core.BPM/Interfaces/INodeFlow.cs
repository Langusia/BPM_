namespace Core.BPM.Interfaces;

public interface INodeFlow : INode
{
    List<INode>? GetAvailableNextNodes();
    bool IsValidPlacement();
    
}