namespace Core.BPM.Nodes.Evaluation;

public interface INodeEvaluatorFactory
{
    INodeStateEvaluator CreateEvaluator(INode node);
}
