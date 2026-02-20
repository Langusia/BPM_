using System;
using System.Collections.Generic;
using BPM.Core.Nodes.Evaluation;
using BPM.Core.Definition.Conditions;
using BPM.Core.Events;

namespace BPM.Core.Nodes;

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
