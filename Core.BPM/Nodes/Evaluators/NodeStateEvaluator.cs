using Core.BPM.Interfaces;

namespace Core.BPM.Nodes.Evaluators;

public class NodeStateEvaluator : INodeStateEvaluator
{
    public bool IsCompleted(INode node, List<object> storedEvents)
    {
        return storedEvents.Any(e => e.GetType() == node.CommandType);
    }

    public bool CanExecute(INode node, List<object> storedEvents)
    {
        return node.PrevSteps.All(prev => storedEvents.Any(e => e.GetType() == prev.CommandType));
    }
}