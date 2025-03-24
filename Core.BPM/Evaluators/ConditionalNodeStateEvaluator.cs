using Core.BPM.Attributes;
using Core.BPM.Interfaces;
using Core.BPM.Nodes;
using Core.BPM.Persistence;

namespace Core.BPM.Evaluators;

public class ConditionalNodeStateEvaluator(INode node, IBpmRepository repository) : INodeStateEvaluator
{
    private List<(bool isComplete, List<INode> availableNodes)>? _ifNodeRootsCompletionStates;
    private List<(bool isComplete, List<INode> availableNodes)>? _elseNodeRootsCompletionStates;

    public bool IsCompleted(List<object> storedEvents)
    {
        if (node is ConditionalNode conditionalNode)
        {
            var aggregate = repository.AggregateOrDefaultStreamFromRegistry(conditionalNode.AggregateCondition.ConditionalAggregateType, storedEvents);
            if (conditionalNode.AggregateCondition.EvaluateAggregateCondition(aggregate))
            {
                if (_ifNodeRootsCompletionStates is null)
                {
                    _ifNodeRootsCompletionStates = new List<(bool isComplete, List<INode> availableNodes)>();
                    conditionalNode.IfNodeRoots.ForEach(x => { _ifNodeRootsCompletionStates.Add(x.GetCheckBranchCompletionAndGetAvailableNodesFromCache(storedEvents)); });
                }

                return _ifNodeRootsCompletionStates.Any(x => x.isComplete);
            }

            if (conditionalNode.ElseNodeRoots is not null)
            {
                if (_elseNodeRootsCompletionStates is null)
                {
                    _elseNodeRootsCompletionStates = new List<(bool isComplete, List<INode> availableNodes)>();
                    conditionalNode.IfNodeRoots.ForEach(x => { _elseNodeRootsCompletionStates.Add(x.GetCheckBranchCompletionAndGetAvailableNodesFromCache(storedEvents)); });
                }

                return _elseNodeRootsCompletionStates.Any(x => x.isComplete);
            }

            return Helpers.FindFirstNonOptionalCompletion(node.PrevSteps, storedEvents) ?? true;
        }

        return false;
    }

    //checks if can execute and returns available nodes for exec
    //returns CanExecute,AvailableNodes
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
                if (_ifNodeRootsCompletionStates is null)
                {
                    _ifNodeRootsCompletionStates = new List<(bool isComplete, List<INode> availableNodes)>();
                    conditionalNode.IfNodeRoots.ForEach(x => { _ifNodeRootsCompletionStates.Add(x.GetCheckBranchCompletionAndGetAvailableNodesFromCache(storedEvents)); });
                }

                return (true, _ifNodeRootsCompletionStates.SelectMany(x => x.availableNodes).ToList());
            }

            if (conditionalNode.ElseNodeRoots is not null)
            {
                if (_elseNodeRootsCompletionStates is null)
                {
                    _elseNodeRootsCompletionStates = new List<(bool isComplete, List<INode> availableNodes)>();
                    conditionalNode.IfNodeRoots.ForEach(x => { _elseNodeRootsCompletionStates.Add(x.GetCheckBranchCompletionAndGetAvailableNodesFromCache(storedEvents)); });
                }

                return (true, _elseNodeRootsCompletionStates.SelectMany(x => x.availableNodes).ToList());
            }
        }

        return (false, []);
    }
}