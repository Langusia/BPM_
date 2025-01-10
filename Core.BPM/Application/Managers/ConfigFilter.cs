using Core.BPM.Interfaces;
using Core.BPM.Nodes;
using Microsoft.CodeAnalysis.Options;

namespace Core.BPM.Application.Managers;

public class ConfigFilter(BProcess process)
{
    private BProcess bProcess = process;
    private List<List<INode>> filtered = process.AllPossibles;

    public List<INode> FilterPossibles(List<INode> storedNodes)
    {
        storedNodes = storedNodes.Where(x => x is not IOptional).ToList();
        var storedGroups = storedNodes.GroupBy(x => x is IMulti ? x.CommandType.Name : Guid.NewGuid().ToString())
            .Select(x => new
            {
                node = x.First(),
                count = x.Count()
            });

        var ct = storedGroups.Count();

        var curs = process.AllPossibles.Where(x =>
            x.Except(x.Where(node => node is IOptional))
                .Take(ct)
                .SequenceEqual(storedGroups.Select(c => c.node).ToList(), new NodeEqualityComparer())).ToList();

        var rrs = curs.SelectMany(x =>
        {
            var ix = x.IndexOf(x.First(z => z.CommandType == storedGroups.Last().node.CommandType));
            return x[ix].NextSteps.Union(x.Where((value, index) => index <= ix && value is IOptional or IMulti));
        }).ToList();


        return rrs;
    }

    public List<INode?>? GetNextPossibles(List<List<INode>> allCurrentMandatoryPossibles, List<INode> storedNodes)
    {
        storedNodes = storedNodes.Where(x => x is not IOptional).ToList();
        var storedGroups = storedNodes.GroupBy(x => x is IMulti ? x.CommandType.Name : Guid.NewGuid().ToString())
            .Select(x => new
            {
                node = x.First(),
                count = x.Count()
            });
        var ct = storedGroups.Count();
        var filtered = process.AllMandatoryPossibles.Where(x =>
            x.Take(ct)
                .SequenceEqual(storedGroups.Select(x => x.node).ToList(), new NodeEqualityComparer())).ToList();

        return filtered.SelectMany(x => x?.Take(ct).Where(z => z is IMulti || z is IOptional).Last().NextSteps).ToList();
    }
}

public static class ConfigFilterExtensions
{
    public static List<INode> Filter(this BProcess process, List<INode> storedNodes)
    {
        var filter = new ConfigFilter(process);
        return filter.FilterPossibles(storedNodes);
    }
}

public interface IFilterable
{
    List<List<INode>> Filter(List<List<INode>> filterFrom, List<INode> storedNodes);
}

public class NodeEqualityComparer : IEqualityComparer<INode>
{
    public bool Equals(INode? x, INode? y)
    {
        return x?.CommandType == y?.CommandType;
    }

    public int GetHashCode(INode obj) => obj.GetHashCode();
}