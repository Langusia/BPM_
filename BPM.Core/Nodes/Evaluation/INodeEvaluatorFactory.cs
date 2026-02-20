namespace BPM.Core.Nodes.Evaluation;

public interface INodeEvaluatorFactory
{
    INodeStateEvaluator CreateEvaluator(INode node);
}
