using Core.BPM.Attributes;
using Core.BPM.Interfaces;

namespace Core.BPM.Evaluators;

public class AnyTimeNodeStateEvaluator(INode node) : INodeStateEvaluator
{
    public bool IsCompleted(List<object> storedEvents)
    {
        var bpmEvents = storedEvents.OfType<BpmEvent>();
        return bpmEvents.Any(node.ContainsNodeEvent);
    }

    public (bool, List<INode>) CanExecute(INode rootNode, List<object> storedEvents)
    {
        bool canExecute = !rootNode.NextSteps?.Where(z => z.CommandType != node.CommandType).Any(x => x.ContainsEvent(storedEvents)) ?? true;
        if (canExecute)
        {
            canExecute = Helpers.FindFirstNonOptionalCompletion(node.PrevSteps, storedEvents) ?? true;
            if (!canExecute)
                return (false, []);

            return (true, [node]);
        }

        return (false, []);
    }
}