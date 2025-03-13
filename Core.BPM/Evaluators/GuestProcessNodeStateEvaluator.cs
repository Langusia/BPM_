using Core.BPM.Attributes;
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
            processNode.ProcessConfig.RootNode.GetCheckBranchCompletionAndGetAvailableNodesFromCache(storedEvents);
            ((IAggregate)Activator.CreateInstance(processNode.ProcessType)).IsCompleted();
        }

        throw new NotImplementedException();
    }

    public (bool canExec, List<INode> availableNodes) CanExecute(List<object> storedEvents)
    {
        bool canExecute = Helpers.FindFirstNonOptionalCompletion(node.PrevSteps, storedEvents) ?? true;
        if (!canExecute)
            return (false, []);

        if (node is GuestProcessNode processNode)
        {
            List<INode> result = [];
            result.AddRange(processNode.ProcessConfig.RootNode.GetCheckBranchCompletionAndGetAvailableNodesFromCache(storedEvents).availableNodes);
            return (canExecute, result);
        }

        return (false, []);
    }
}