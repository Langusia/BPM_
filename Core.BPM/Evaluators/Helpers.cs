﻿using Core.BPM.Interfaces;
using Core.BPM.Nodes;

namespace Core.BPM.Evaluators;

public static class Helpers
{
    public static bool? FindFirstNonOptionalCompletion(List<INode>? prevSteps, List<object> storedEvents)
    {
        if (prevSteps == null || prevSteps.Count == 0 || prevSteps.All(x => x is null))
            return false;

        if (prevSteps.Where(x => x is not OptionalNode).Any(x => x.GetEvaluator().IsCompleted(storedEvents)))
            return true;

        var optionalNodes = prevSteps.Where(x => x is OptionalNode);
        foreach (var prev in optionalNodes)
        {
            var result = FindFirstNonOptionalCompletion(prev.PrevSteps, storedEvents);
            if (result.HasValue)
                return result;
        }

        return false;
    }
}