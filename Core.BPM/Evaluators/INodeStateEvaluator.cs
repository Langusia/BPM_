using Core.BPM.Interfaces;
using Core.BPM.Nodes;

namespace Core.BPM.Evaluators;

public interface INodeStateEvaluator
{
    bool IsCompleted(List<object> storedEvents);
    (bool canExec, List<INode> availableNodes) CanExecute(List<object> storedEvents);

    
}