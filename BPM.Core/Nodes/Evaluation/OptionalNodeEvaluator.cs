using System.Collections.Generic;

namespace BPM.Core.Nodes.Evaluation;

public class OptionalNodeEvaluator(INode node) : INodeStateEvaluator
{
    public bool IsCompleted(List<object> storedEvents)
    {
        return true;
    }

    public (bool canExec, List<INode> availableNodes) CanExecute(INode rootNode,List<object> storedEvents)
    {
        bool canExecute = Helpers.FindFirstNonOptionalCompletion(node.PrevSteps, storedEvents) ?? true;
        if (!canExecute)
            return (false, []);

        return (true, [node]);
    }
}
