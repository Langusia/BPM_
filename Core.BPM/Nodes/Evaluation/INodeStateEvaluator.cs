using System.Collections.Generic;

namespace Core.BPM.Nodes.Evaluation;

public interface INodeStateEvaluator
{
    bool IsCompleted(List<object> storedEvents);
    (bool canExec, List<INode> availableNodes) CanExecute(INode rootNode, List<object> storedEvents);
}
