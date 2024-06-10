﻿namespace Core.BPM.Interfaces;

public interface INode
{
    Type CommandType { get; }
    Type ProcessType { get; }
    bool AnyTime { get; set; }

    List<INode>? NextSteps { get; set; }
    void AddNextStep(INode node);
    void AddNextStepToTail(INode node);
    List<INode>? PrevSteps { get; set; }
    void AddPrevStep(INode node);
}