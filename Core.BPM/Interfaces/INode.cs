using Core.BPM.BCommand;

namespace Core.BPM.Interfaces;

public interface INode
{
    List<string> LoadEvents();

    Type CommandType { get; }
    Type ProcessType { get; }
    BpmEventOptions Options { get; set; }

    List<INode>? NextSteps { get; set; }
    void AddNextStep(INode node);
    void AddNextStepToTail(INode node);
    List<INode>? PrevSteps { get; set; }
    void AddPrevStep(INode node);
}