using Core.BPM.Interfaces;

namespace Core.BPM.Evaluators;

public class GroupNodeStateEvaluator(INode node) : INodeStateEvaluator
{
    private readonly Dictionary<(INode, int), (bool, List<INode>)> _cache = new();

    private (bool isComplete, List<INode> availableNodes) GetCheckBranchCompletionAndGetAvailableNodesFromCache(INode node, List<object> storedEvents)
    {
        int eventHash = storedEvents?.Count > 0 ? storedEvents.GetHashCode() : 0;
        var cacheKey = (node, eventHash);

        if (_cache.TryGetValue(cacheKey, out var cachedResult))
            return cachedResult;

        return node.CheckBranchCompletionAndGetAvailableNodes(node, storedEvents);
    }

    public bool IsCompleted(List<object> storedEvents)
    {
        if (node is GroupNode groupNode)
        {
            return groupNode.SubRootNodes.All(x => GetCheckBranchCompletionAndGetAvailableNodesFromCache(x, storedEvents).isComplete);
        }

        return false;
    }

    public (bool, List<INode>) CanExecute(List<object> storedEvents)
    {
        bool canExecute = node.PrevSteps?.Any(prev => prev.GetEvaluator().IsCompleted(storedEvents)) ?? true;
        if (node is GroupNode groupNode)
        {
            List<INode> result = [];
            groupNode.SubRootNodes.ForEach(x => result.AddRange(GetCheckBranchCompletionAndGetAvailableNodesFromCache(x, storedEvents).availableNodes));
            return (canExecute, result);
        }

        return (false, []);
    }
}