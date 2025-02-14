﻿using Core.BPM.Interfaces;

namespace Core.BPM.Evaluators;

public class NodeStateEvaluator(INode node) : INodeStateEvaluator
{
    public bool IsCompleted(List<object> storedEvents)
    {
        return storedEvents.Any(node.ContainsEvent);
    }

    public (bool, List<INode>) CanExecute(List<object> storedEvents)
    {
        return ((node.PrevSteps?.Any(prev => prev.GetEvaluator().IsCompleted(storedEvents)) ?? true)
                && !storedEvents.Any(x => node.ContainsEvent(x)), [node]);
    }
}