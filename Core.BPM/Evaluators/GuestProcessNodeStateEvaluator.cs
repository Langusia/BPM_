using Core.BPM.Configuration;
using Core.BPM.Exceptions;
using Core.BPM.Interfaces;
using Core.BPM.Nodes;
using Core.BPM.Persistence;

namespace Core.BPM.Evaluators;

public class GuestProcessNodeStateEvaluator(INode node, IBpmRepository repository) : INodeStateEvaluator
{
    private Aggregate? _aggregate;
    private (bool isComplete, List<INode> availableNodes) ? _completionState;

    public bool IsCompleted(List<object> storedEvents)
    {
        if (node is GuestProcessNode processNode)
        {
            var config = BProcessGraphConfiguration.GetConfig(processNode.GuestProcessType.Name);
            if (config is null)
                throw new Exception();


            if (repository.TryAggregateAs(processNode.GuestProcessType, storedEvents, out _aggregate))
            {
                _completionState ??= config.RootNode.GetCheckBranchCompletionAndGetAvailableNodesFromCache(storedEvents);
                var explicitCompletion = _aggregate!.IsCompleted();

                return explicitCompletion ?? _completionState.Value.isComplete;
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
                var explicitCompletion = _aggregate?.IsCompleted();

                List<INode> result = [];
                _completionState ??= config.RootNode.GetCheckBranchCompletionAndGetAvailableNodesFromCache(storedEvents);
                result.AddRange(_completionState.Value.availableNodes);

                if (explicitCompletion.HasValue)
                    return (canExecute, explicitCompletion.Value && processNode.SealedSteps ? [] : result);

                return (canExecute, _completionState.Value.isComplete && processNode.SealedSteps ? [] : result);
            }

            return (false, []);
        }

        return (false, []);
    }
}