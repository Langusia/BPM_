using Core.BPM.Attributes;
using Core.BPM.Interfaces;
using Core.BPM.Nodes;

namespace Core.BPM.Evaluators;

public class GroupNodeStateEvaluator(INode node) : INodeStateEvaluator
{
    private List<(bool isComplete, List<INode> availableNodes)>? _subNodesCompletionStates;

    public bool IsCompleted(List<object> storedEvents)
    {
        if (node is GroupNode groupNode)
        {
            if (_subNodesCompletionStates is null)
            {
                _subNodesCompletionStates = new List<(bool isComplete, List<INode> availableNodes)>();
                groupNode.SubRootNodes.ForEach(x => { _subNodesCompletionStates.Add(x.GetCheckBranchCompletionAndGetAvailableNodesFromCache(storedEvents)); });
            }

            return _subNodesCompletionStates.All(x => x.isComplete);
        }

        return false;
    }

    public (bool, List<INode>) CanExecute(List<object> storedEvents)
    {
        bool canExecute = Helpers.FindFirstNonOptionalCompletion(node.PrevSteps, storedEvents) ?? true;
        if (!canExecute)
            return (false, []);

        if (node is GroupNode groupNode)
        {
            List<INode> result = [];

            if (_subNodesCompletionStates is null)
            {
                _subNodesCompletionStates = new List<(bool isComplete, List<INode> availableNodes)>();
                groupNode.SubRootNodes.ForEach(x => { _subNodesCompletionStates.Add(x.GetCheckBranchCompletionAndGetAvailableNodesFromCache(storedEvents)); });
            }

            result.AddRange(_subNodesCompletionStates.SelectMany(x => x.availableNodes));
            return (canExecute, result);
        }

        return (false, []);
    }
}