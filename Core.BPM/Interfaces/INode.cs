using Core.BPM.Application.Managers;
using Core.BPM.Attributes;
using Core.BPM.BCommand;

namespace Core.BPM.Interfaces;

public interface INode
{
    Type CommandType { get; }
    string CommandName { get; }
    Type ProcessType { get; }
    BpmEventOptions Options { get; set; }

    public List<Type> ProducingEvents { get; }
    List<INode>? NextSteps { get; set; }
    void AddNextStep(INode node);
    void AddNextStepToTail(INode node);
    List<INode>? PrevSteps { get; set; }
    void AddPrevStep(INode node);
    INode? FindNextNode(string eventName);
    public BpmProducer CommandProducer() => (BpmProducer)CommandType.GetCustomAttributes(typeof(BpmProducer), false).FirstOrDefault()!;

    public abstract bool ValidatePlacement(List<string> savedEvents, INode? currentNode);
}