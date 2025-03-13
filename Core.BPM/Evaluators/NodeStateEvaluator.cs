using Core.BPM.Attributes;
using Core.BPM.Interfaces;

namespace Core.BPM.Evaluators;

public class NodeStateEvaluator(INode node) : INodeStateEvaluator
{
    public bool IsCompleted(List<object> storedEvents)
    {
        var bpmEvents = storedEvents.OfType<BpmEvent>();
        return bpmEvents.Any(node.ContainsNodeEvent);
    }

    public (bool, List<INode>) CanExecute(List<object> storedEvents)
    {
        var bpmEvents = storedEvents.OfType<BpmEvent>();
        if (node.PrevSteps is null || node.PrevSteps.All(x => x == null))
            return (!bpmEvents.Any(x => node.ContainsNodeEvent(x)), [node]);

        bool canExecute = Helpers.FindFirstNonOptionalCompletion(node.PrevSteps, storedEvents) ?? true;
        if (!canExecute)
            return (false, []);

        return (canExecute && !bpmEvents.Any(x => node.ContainsNodeEvent(x)), [node]);
    }
}