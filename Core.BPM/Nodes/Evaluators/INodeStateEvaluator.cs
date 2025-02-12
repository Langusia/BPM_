using Core.BPM.Interfaces;

namespace Core.BPM.Nodes.Evaluators;

public interface INodeStateEvaluator
{
    bool IsCompleted(INode node, List<object> storedEvents);
    bool CanExecute(INode node, List<object> storedEvents);
}