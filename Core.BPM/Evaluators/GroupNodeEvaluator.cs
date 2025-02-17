using Core.BPM.Interfaces;
using Core.BPM.Nodes;

namespace Core.BPM.Evaluators;

public class GroupNodeStateEvaluator(INode node) : INodeStateEvaluator
{
    public bool IsCompleted(List<object> storedEvents)
    {
        if (node is GroupNode groupNode)
        {
            return groupNode.SubRootNodes.All(x => x.GetCheckBranchCompletionAndGetAvailableNodesFromCache(storedEvents).isComplete);
        }

        return false;
    }

    public (bool, List<INode>) CanExecute(List<object> storedEvents)
    {
        bool canExecute = node.PrevSteps?.Any(prev => prev.GetEvaluator().IsCompleted(storedEvents)) ?? true;
        if (node is GroupNode groupNode)
        {
            List<INode> result = [];
            groupNode.SubRootNodes.ForEach(x => result.AddRange(x.GetCheckBranchCompletionAndGetAvailableNodesFromCache(storedEvents).availableNodes));
            return (canExecute, result);
        }

        return (false, []);
    }
}