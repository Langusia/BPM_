using Core.BPM.Interfaces;
using Core.BPM.Nodes;
using Core.BPM.Persistence;

namespace Core.BPM.Evaluators.Factory;

public class NodeEvaluatorFactory(IBpmRepository repository) : INodeEvaluatorFactory
{
    public INodeStateEvaluator CreateEvaluator(INode node)
    {
        return node switch
        {
            ConditionalNode conditionalNode =>
                new ConditionalNodeStateEvaluator(conditionalNode, repository),

            GroupNode groupNode =>
                new GroupNodeStateEvaluator(groupNode),

            AnyTimeNode defaultNode =>
                new AnyTimeNodeStateEvaluator(defaultNode),

            OptionalNode defaultNode =>
                new OptionalNodeEvaluator(defaultNode),

            Node defaultNode =>
                new NodeStateEvaluator(defaultNode),

            _ => new NodeStateEvaluator(node)
        };
    }
}