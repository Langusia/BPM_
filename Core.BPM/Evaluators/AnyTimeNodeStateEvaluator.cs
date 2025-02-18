using Core.BPM.Interfaces;

namespace Core.BPM.Evaluators;

public class AnyTimeNodeStateEvaluator(INode node) : INodeStateEvaluator
{
    public bool IsCompleted(List<object> storedEvents)
    {
        return storedEvents.Any(node.ContainsEvent);
    }

    public (bool, List<INode>) CanExecute(List<object> storedEvents)
    {
        bool canExecute = Helpers.FindFirstNonOptionalCompletion(node.PrevSteps, storedEvents) ?? true;
        if (!canExecute)
            return (false, []);

        return (true, [node]);
    }
}