using Core.BPM.Interfaces;

namespace Core.BPM.Evaluators;

public class NodeStateEvaluator(INode node) : INodeStateEvaluator
{
    public bool IsCompleted(List<object> storedEvents)
    {
        return storedEvents.Any(node.ContainsEvent);
    }

    public (bool, List<INode>) CanExecute(List<object> storedEvents)
    {
        if (node.PrevSteps is null || node.PrevSteps.All(x => x == null))
            return (!storedEvents.Any(x => node.ContainsEvent(x)), [node]);

        return ((node.PrevSteps?.Where(x => x is not null).Any(prev => prev.GetEvaluator().IsCompleted(storedEvents)) ?? true)
                && !storedEvents.Any(x => node.ContainsEvent(x)), [node]);
    }
}