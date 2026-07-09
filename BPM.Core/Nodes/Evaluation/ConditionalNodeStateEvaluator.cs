using System.Collections.Generic;
using System.Linq;
using BPM.Core.Persistence;

namespace BPM.Core.Nodes.Evaluation;

/// <summary>
/// Phase 1.2: with an <see cref="IReplayContext"/> present, aggregate
/// rehydration and branch traversals are shared per (request, stream) instead
/// of replayed per node visit. The old per-instance memo fields are gone —
/// they only lived for a single visit and would have served stale state if an
/// instance were ever reused across streams. Without a context (legacy
/// construction), behavior is exactly the pre-1.2 repository path.
/// </summary>
public class ConditionalNodeStateEvaluator(INode node, IBpmRepository repository, IReplayContext? context = null) : INodeStateEvaluator
{
    public bool IsCompleted(List<object> storedEvents)
    {
        if (node is ConditionalNode conditionalNode)
        {
            var aggregate = GetAggregate(conditionalNode, storedEvents);
            if (conditionalNode.AggregateCondition.EvaluateAggregateCondition(aggregate))
                return BranchStates(conditionalNode.IfNodeRoots, storedEvents).Any(x => x.isComplete);

            if (conditionalNode.ElseNodeRoots is not null)
                return BranchStates(conditionalNode.ElseNodeRoots, storedEvents).Any(x => x.isComplete);

            return Helpers.FindFirstNonOptionalCompletion(node.PrevSteps, storedEvents) ?? true;
        }

        return false;
    }

    //checks if can execute and returns available nodes for exec
    //returns CanExecute,AvailableNodes
    public (bool, List<INode>) CanExecute(INode rootNode, List<object> storedEvents)
    {
        bool canExecute = !rootNode.NextSteps?.Where(z => z.CommandType != node.CommandType).Any(x => x.ContainsEvent(storedEvents)) ?? true;
        if (canExecute)
        {
            canExecute = (node.PrevSteps?.All(x => x is null) ?? true) || (Helpers.FindFirstNonOptionalCompletion(node.PrevSteps?.Where(x => x is not null).ToList(), storedEvents) ?? true);
            if (!canExecute)
                return (false, []);

            if (node is ConditionalNode conditionalNode)
            {
                var aggregate = GetAggregate(conditionalNode, storedEvents);
                if (conditionalNode.AggregateCondition.EvaluateAggregateCondition(aggregate))
                    return (true, BranchStates(conditionalNode.IfNodeRoots, storedEvents).SelectMany(x => x.availableNodes).ToList());

                if (conditionalNode.ElseNodeRoots is not null)
                    return (true, BranchStates(conditionalNode.ElseNodeRoots, storedEvents).SelectMany(x => x.availableNodes).ToList());
            }

            return (false, []);
        }

        return (false, []);
    }

    private object GetAggregate(ConditionalNode conditionalNode, List<object> storedEvents) =>
        context is null
            ? repository.AggregateOrDefaultStreamFromRegistry(conditionalNode.AggregateCondition.ConditionalAggregateType, storedEvents)
            : context.GetAggregate(conditionalNode.AggregateCondition.ConditionalAggregateType, storedEvents);

    private List<(bool isComplete, List<INode> availableNodes)> BranchStates(List<INode> roots, List<object> storedEvents) =>
        roots.Select(root => context is null
                ? root.GetCheckBranchCompletionAndGetAvailableNodesFromCache(storedEvents)
                : context.GetOrAddTraversal(root, storedEvents, () => root.GetCheckBranchCompletionAndGetAvailableNodesFromCache(storedEvents)))
            .ToList();
}
