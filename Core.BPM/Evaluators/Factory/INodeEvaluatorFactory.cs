using Core.BPM.Interfaces;

namespace Core.BPM.Evaluators.Factory;

public interface INodeEvaluatorFactory
{
    INodeStateEvaluator CreateEvaluator(INode node);
}