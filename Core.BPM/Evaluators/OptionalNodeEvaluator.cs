using Core.BPM.Interfaces;

namespace Core.BPM.Evaluators;

public class OptionalNodeEvaluator(INode node) : INodeStateEvaluator
{
    public bool IsCompleted(List<object> storedEvents)
    {
        return true;
    }

    public (bool canExec, List<INode> availableNodes) CanExecute(List<object> storedEvents)
    {
        return (node.PrevSteps?.Any(prev => prev.GetEvaluator().IsCompleted(storedEvents)) ?? true, [node]);
    }
}