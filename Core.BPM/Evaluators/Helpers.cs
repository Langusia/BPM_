using Core.BPM.Interfaces;
using Core.BPM.Nodes;

namespace Core.BPM.Evaluators;

public static class Helpers
{
    public static bool? FindFirstNonOptionalCompletion(List<INode>? prevSteps, List<object> storedEvents)
    {
        if (prevSteps == null || prevSteps.Count == 0 || prevSteps.All(x => x is null))
            return null; // No predecessors to check

        foreach (var prev in prevSteps)
        {
            if (!(prev is OptionalNode))
                return prev.GetEvaluator().IsCompleted(storedEvents);

            var result = FindFirstNonOptionalCompletion(prev.PrevSteps, storedEvents);
            if (result.HasValue) // Stop at the first found result
                return result;
        }

        return null; // No non-optional node found
    }
}