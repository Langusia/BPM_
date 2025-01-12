using Core.BPM.Interfaces;
using Core.BPM.Nodes;
using Core.BPM.Persistence;

namespace Core.BPM.Application.Managers;

public class ConfigManager(IBpmRepository repository)
{
    public List<INode>? UnlockedPaths(List<object> events, BProcess process)
    {
        var allEvents = events.Select(x => x.GetType().Name);

        //map events to Nodes
        var rs = allEvents.Select(x =>
                process.AllDistinctCommands.FirstOrDefault(z => z.ProducingEvents.Select(z => z.Name).ToList().Contains(x)))
            .Where(x => x is not null).ToList();

        var filtered = FilterPossibles(rs, process);

        var nodesPredicates = filtered.Select(x => x.AggregateConditions);
        var predicateAggregateTypes = nodesPredicates.SelectMany(x => x.Select(z => z.ConditionalAggregateType)).Distinct();
        Dictionary<Type, object> aggregateDictionary = [];
        foreach (var predicateAggregateType in predicateAggregateTypes)
        {
            aggregateDictionary.Add(predicateAggregateType, repository.AggregateStreamFromRegistry(predicateAggregateType, allEvents));
        }

        if (nodesPredicates.Any())
        {
            foreach (var nodePredicates in nodesPredicates)
            {
                foreach (var nodePredicate in nodePredicates)
                {
                    nodePredicate.EvaluateAggregateCondition(aggregateDictionary[nodePredicate.ConditionalAggregateType]);
                }
            }
        }

        return filtered;
    }

    private List<INode> FilterPossibles(List<INode> storedNodes, BProcess process)
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
}