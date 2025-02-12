using Core.BPM.Evaluators;
using Core.BPM.AggregateConditions;

namespace Core.BPM.Interfaces;

public interface INode
{
    Type CommandType { get; }
    List<IAggregateCondition>? AggregateConditions { get; set; }
    public List<Type> ProducingEvents { get; }
    List<INode>? NextSteps { get; set; }
    void AddNextStep(INode node);
    List<INode>? PrevSteps { get; set; }
    void SetPrevSteps(List<INode>? nodes);
    INode? FindNextNode(string eventName);
    INodeStateEvaluator GetEvaluator();
}