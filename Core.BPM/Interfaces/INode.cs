using System;
using System.Collections.Generic;
using Core.BPM.Evaluators;
using Core.BPM.AggregateConditions;
using Core.BPM.Application.Events;
using Core.BPM.Attributes;

namespace Core.BPM.Interfaces;

public interface INode
{
    Type CommandType { get; }
    int NodeLevel { get; set; }
    List<IAggregateCondition>? AggregateConditions { get; set; }
    public List<Type> ProducingEvents { get; }
    List<INode>? NextSteps { get; set; }
    void AddNextStep(INode node);
    List<INode?>? PrevSteps { get; set; }
    void SetPrevSteps(List<INode>? nodes);
    INode? FindNextNode(string eventName);
    INodeStateEvaluator GetEvaluator();
    bool ContainsEvent(object @event);
    bool ContainsEvent(List<object> @events);
    bool ContainsNodeEvent(BpmEvent @event);
    List<INode> GetAllNodes();

    (bool isComplete, List<INode> availableNodes) CheckBranchCompletionAndGetAvailableNodes(INode start, List<object> storedEvents,
        List<(string, INode, bool isCompleted, bool canExec, List<INode> availableNodes)>? res = null);

    (bool isComplete, List<INode> availableNodes) GetCheckBranchCompletionAndGetAvailableNodesFromCache(List<object> storedEvents,
        List<(string, INode, bool isCompleted, bool canExec, List<INode> availableNodes)>? res = null);
}