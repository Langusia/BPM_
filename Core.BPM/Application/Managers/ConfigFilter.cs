using Core.BPM.Interfaces;
using Core.BPM.Nodes;
using Microsoft.CodeAnalysis.Options;

namespace Core.BPM.Application.Managers;

public class ConfigFilter(BProcess process)
{
    
}

public static class ConfigFilterExtensions
{
    public static List<INode> Filter(this BProcess process, List<INode> storedNodes)
    {
        var filter = new ConfigFilter(process);
        return filter.FilterPossibles(storedNodes);
    }
}

public class NodeEqualityComparer : IEqualityComparer<INode>
{
    public bool Equals(INode? x, INode? y)
    {
        return x?.CommandType == y?.CommandType;
    }

    public int GetHashCode(INode obj) => obj.GetHashCode();
}