using Core.BPM.Attributes;
using Core.BPM.Interfaces;

namespace Core.BPM.Evaluators;

public class NodeStateEvaluator(INode node) : INodeStateEvaluator
{
    private bool _containsNode;

    public bool IsCompleted(List<object> storedEvents)
    {
        var bpmEvents = storedEvents.OfType<BpmEvent>();
        _containsNode = bpmEvents.Any(node.ContainsNodeEvent);
        return _containsNode;
    }

    public (bool, List<INode>) CanExecute(INode rootNode, List<object> storedEvents)
    {
        if (node.PrevSteps is null || node.PrevSteps.All(x => x == null))
            return (!_containsNode, [node]);

        bool canExecute = !rootNode.NextSteps?.Where(z => z.CommandType != node.CommandType).Any(x => x.ContainsEvent(storedEvents)) ?? true;
        if (canExecute)
        {
            canExecute = Helpers.FindFirstNonOptionalCompletion(node.PrevSteps, storedEvents) ?? true;
            if (!canExecute)
                return (false, []);

            return (canExecute && !_containsNode, [node]);
        }

        return (false, []);
    }
}