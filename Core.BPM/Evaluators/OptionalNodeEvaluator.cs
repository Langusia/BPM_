using Core.BPM.Interfaces;
using Core.BPM.Nodes;

namespace Core.BPM.Evaluators;

public class OptionalNodeEvaluator(INode node) : INodeStateEvaluator
{
    public bool IsCompleted(List<object> storedEvents)
    {
        return true;
    }

    public (bool canExec, List<INode> availableNodes) CanExecute(List<object> storedEvents)
    {
        bool canExecute = Helpers.FindFirstNonOptionalCompletion(node.PrevSteps, storedEvents) ?? true;
        if (!canExecute)
            return (false, []);

        return (true, [node]);
    }
}