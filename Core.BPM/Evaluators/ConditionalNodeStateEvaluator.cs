using Core.BPM.Interfaces;
using Core.BPM.Nodes;

namespace Core.BPM.Evaluators;

public class ConditionalNodeStateEvaluator(INode node) : INodeStateEvaluator
{
    public bool IsCompleted(List<object> storedEvents)
    {
        if (node is ConditionalNode conditionalNode)
        {
            return conditionalNode.IfRootNode.CheckBranchCompletionAndGetAvailableNodes(conditionalNode.IfRootNode, storedEvents).isComplete ||
                   conditionalNode.ElseRootNode.CheckBranchCompletionAndGetAvailableNodes(conditionalNode.IfRootNode, storedEvents).isComplete;
        }

        return false;
    }

    public (bool, List<INode>) CanExecute(List<object> storedEvents)
    {
        return (node.PrevSteps?.Any(prev => prev.GetEvaluator().IsCompleted(storedEvents)) ?? true, [node]);
    }
}