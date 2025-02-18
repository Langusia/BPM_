using Core.BPM.Interfaces;
using Core.BPM.Nodes;
using Core.BPM.Persistence;

namespace Core.BPM.Evaluators;

public class ConditionalNodeStateEvaluator(INode node, IBpmRepository repository) : INodeStateEvaluator
{
    public bool IsCompleted(List<object> storedEvents)
    {
        if (node is ConditionalNode conditionalNode)
        {
            var aggregate = repository.AggregateOrDefaultStreamFromRegistry(conditionalNode.AggregateCondition.ConditionalAggregateType, storedEvents);
            if (conditionalNode.AggregateCondition.EvaluateAggregateCondition(aggregate))
            {
                return conditionalNode.IfNodeRoots.Any(x => x.GetCheckBranchCompletionAndGetAvailableNodesFromCache(storedEvents).isComplete);
            }

            if (conditionalNode.ElseNodeRoots is not null)
                return conditionalNode.ElseNodeRoots?.Any(x => x.GetCheckBranchCompletionAndGetAvailableNodesFromCache(storedEvents).isComplete) ?? false;

            return Helpers.FindFirstNonOptionalCompletion(node.PrevSteps, storedEvents) ?? true;
        }

        return false;
    }

    public (bool, List<INode>) CanExecute(List<object> storedEvents)
    {
        bool canExecute = Helpers.FindFirstNonOptionalCompletion(node.PrevSteps, storedEvents) ?? true;
        if (!canExecute)
            return (false, []);

        if (node is ConditionalNode conditionalNode)
        {
            var aggregate = repository.AggregateOrDefaultStreamFromRegistry(conditionalNode.AggregateCondition.ConditionalAggregateType, storedEvents);
            if (conditionalNode.AggregateCondition.EvaluateAggregateCondition(aggregate))
            {
                var results = conditionalNode.IfNodeRoots.Select(x => x.GetCheckBranchCompletionAndGetAvailableNodesFromCache(storedEvents)).ToList();
                return (true, results.SelectMany(x => x.availableNodes).ToList());
            }

            if (conditionalNode.ElseNodeRoots is not null)
            {
                var elseResults = conditionalNode.ElseNodeRoots?.Select(x => x.GetCheckBranchCompletionAndGetAvailableNodesFromCache(storedEvents)).ToList();
                return (true, elseResults.SelectMany(x => x.availableNodes).ToList());
            }
        }

        return (false, []);
    }
}