using System;
using System.Collections.Generic;
using System.Linq;
using BPM.Core.Configuration;
using BPM.Core.Exceptions;
using BPM.Core.Process;
using BPM.Core.Persistence;

namespace BPM.Core.Nodes.Evaluation;

/// <summary>
/// Phase 1.2: with an <see cref="IReplayContext"/> present, the guest aggregate
/// and the guest-graph traversal are shared per (request, stream) instead of
/// replayed per node visit. The old per-instance memo fields (_aggregate,
/// _completionState) are gone. Without a context (legacy construction),
/// behavior is exactly the pre-1.2 repository path.
/// </summary>
public class GuestProcessNodeStateEvaluator(INode node, IBpmRepository repository, IReplayContext? context = null) : INodeStateEvaluator
{
    private Aggregate? _aggregate;
    private bool _aggregateResolved;
    private (bool isComplete, List<INode> availableNodes)? _completionState;

    public bool IsCompleted(List<object> storedEvents)
    {
        if (node is GuestProcessNode processNode)
        {
            var config = BProcessGraphConfiguration.GetConfig(processNode.GuestProcessType.Name);
            if (config is null)
                throw new Exception();


            if (TryAggregate(processNode, storedEvents, out var aggregate))
            {
                var completionState = GuestCompletion(config, storedEvents);
                var explicitCompletion = aggregate!.IsCompleted();

                return explicitCompletion ?? completionState.isComplete;
            }
        }

        return false;
    }

    public (bool canExec, List<INode> availableNodes) CanExecute(INode rootNode, List<object> storedEvents)
    {
        bool canExecute = !rootNode.NextSteps?.Where(z => z.CommandType != node.CommandType).Any(x => x.ContainsEvent(storedEvents)) ?? true;
        if (canExecute)
        {
            canExecute = Helpers.FindFirstNonOptionalCompletion(node.PrevSteps, storedEvents) ?? true;
            if (!canExecute)
                return (false, []);

            if (node is GuestProcessNode processNode)
            {
                var config = BProcessGraphConfiguration.GetConfig(processNode.GuestProcessType.Name);
                if (config is null)
                    throw new NoDefinitionFoundException(processNode.GuestProcessType.Name);

                TryAggregate(processNode, storedEvents, out var aggregate);
                var explicitCompletion = aggregate?.IsCompleted();

                List<INode> result = [];
                var completionState = GuestCompletion(config, storedEvents);
                result.AddRange(completionState.availableNodes);

                if (explicitCompletion.HasValue)
                    return (canExecute && !explicitCompletion.Value, explicitCompletion.Value && processNode.SealedSteps ? [] : result);

                return (canExecute, completionState.isComplete && processNode.SealedSteps ? [] : result);
            }

            return (false, []);
        }

        return (false, []);
    }

    private bool TryAggregate(GuestProcessNode processNode, List<object> storedEvents, out Aggregate? aggregate)
    {
        if (context is not null)
            return context.TryGetAggregate(processNode.GuestProcessType, storedEvents, out aggregate);

        // Legacy path: per-visit memo, exactly as before 1.2 — including the
        // failure case: a failed attempt is never retried within the visit.
        if (_aggregateResolved)
        {
            aggregate = _aggregate;
            return _aggregate is not null;
        }

        var ok = repository.TryAggregateAs(processNode.GuestProcessType, storedEvents, out _aggregate);
        _aggregateResolved = true;
        aggregate = _aggregate;
        return ok;
    }

    private (bool isComplete, List<INode> availableNodes) GuestCompletion(BProcess config, List<object> storedEvents)
    {
        if (context is not null)
            return context.GetOrAddTraversal(config.RootNode, storedEvents,
                () => config.RootNode.GetCheckBranchCompletionAndGetAvailableNodesFromCache(storedEvents));

        _completionState ??= config.RootNode.GetCheckBranchCompletionAndGetAvailableNodesFromCache(storedEvents);
        return _completionState.Value;
    }
}
