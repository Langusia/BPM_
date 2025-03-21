using Core.BPM.Application;
using Core.BPM.Configuration;
using Core.BPM.Interfaces;
using Core.BPM.Nodes;
using Core.BPM.Persistence;

namespace Core.BPM.Evaluators;

public class GuestProcessNodeStateEvaluator(INode node, IBpmRepository repository) : INodeStateEvaluator
{
    public bool IsCompleted(List<object> storedEvents)
    {
        if (node is GuestProcessNode processNode)
        {
            var config = BProcessGraphConfiguration.GetConfig(processNode.AggregateType.Name);
            if (config is null)
                throw new Exception();

            config.RootNode.GetCheckBranchCompletionAndGetAvailableNodesFromCache(storedEvents);
            ((IAggregate)FastActivator.CreateAggregate(processNode.ProcessType)).IsCompleted();
        }

        return false;
    }

    public (bool canExec, List<INode> availableNodes) CanExecute(List<object> storedEvents)
    {
        bool canExecute = Helpers.FindFirstNonOptionalCompletion(node.PrevSteps, storedEvents) ?? true;
        if (!canExecute)
            return (false, []);

        if (node is GuestProcessNode processNode)
        {
            var config = BProcessGraphConfiguration.GetConfig(processNode.AggregateType.Name);
            if (config is null)
                throw new Exception();

            List<INode> result = [];
            result.AddRange(config.RootNode.GetCheckBranchCompletionAndGetAvailableNodesFromCache(storedEvents).availableNodes);
            return (canExecute, result);
        }

        return (false, []);
    }
}