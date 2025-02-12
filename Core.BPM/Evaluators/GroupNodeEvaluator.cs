using Core.BPM.Interfaces;

namespace Core.BPM.Evaluators;

public class GroupNodeStateEvaluator : INodeStateEvaluator
{
    public bool IsCompleted(INode node, List<object> storedEvents)
    {
        if (node is GroupNode groupNode)
        {
            return groupNode.SubNodes.All(subNode => storedEvents.Any(e => e.GetType() == subNode.CommandType));
        }

        return false;
    }

    public bool CanExecute(INode node, List<object> storedEvents)
    {
        if (node is GroupNode groupNode)
        {
            return groupNode.SubNodes.Any(subNode => storedEvents.Any(e => e.GetType() == subNode.CommandType));
        }

        return false;
    }
}