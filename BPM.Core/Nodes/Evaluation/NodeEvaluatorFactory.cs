using BPM.Core.Persistence;

namespace BPM.Core.Nodes.Evaluation;

public class NodeEvaluatorFactory(IBpmRepository repository) : INodeEvaluatorFactory
{
    public INodeStateEvaluator CreateEvaluator(INode node)
    {
        return node switch
        {
            GuestProcessNode conditionalNode =>
                new GuestProcessNodeStateEvaluator(conditionalNode, repository),

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
