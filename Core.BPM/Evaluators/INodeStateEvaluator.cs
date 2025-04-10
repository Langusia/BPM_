using System.Collections.Generic;
using Core.BPM.Attributes;
using Core.BPM.Interfaces;
using Core.BPM.Nodes;

namespace Core.BPM.Evaluators;

public interface INodeStateEvaluator
{
    bool IsCompleted(List<object> storedEvents);
    (bool canExec, List<INode> availableNodes) CanExecute(INode rootNode, List<object> storedEvents);
}