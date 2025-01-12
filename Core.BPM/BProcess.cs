using Core.BPM.Interfaces;
using Core.BPM.Nodes;
using Core.BPM.Application.Managers;
using Core.BPM.Persistence;
using Core.BPM.Registry;

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

        var curs = this.AllPossibles.Where(x =>
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

    public List<INode>? UnlockedPaths(List<object> events, BProcess process)
    {
        var allEvents = events.Select(x => x.GetType().Name);
        var repo = new BpmRepository(null, null, null);

        List<List<INode>> result = [];

        //map events to Nodes
        var rs = allEvents.Select(x =>
                process.AllDistinctCommands.FirstOrDefault(z => z.ProducingEvents.Select(z => z.Name).ToList().Contains(x)))
            .Where(x => x is not null).ToList();
        var filtered = FilterPossibles(rs);

        var nodesPredicates = filtered.Select(x => new { x, x.AggregateConditions }).ToDictionary(x => x);
        var predicateAggregateTypes = nodesPredicates.SelectMany(x => x.Select(z => z.ConditionalAggregateType)).Distinct();
        Dictionary<Type, object> aggregateDictionary = [];
        foreach (var predicateAggregateType in predicateAggregateTypes)
        {
            aggregateDictionary.Add(predicateAggregateType, repo.AggregateStreamFromRegistryAsync(predicateAggregateType, allEvents));
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