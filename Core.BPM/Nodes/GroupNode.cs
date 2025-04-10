using System;
using System.Collections.Generic;
using System.Linq;
using Core.BPM.Attributes;
using Core.BPM.Evaluators;
using Core.BPM.Evaluators.Factory;
using Core.BPM.Interfaces;

namespace Core.BPM.Nodes;

public class GroupNode(Type processType, INodeEvaluatorFactory nodeEvaluatorFactory) : NodeBase(typeof(GroupNode), processType, nodeEvaluatorFactory)
{
    public List<INode> SubRootNodes { get; set; } = [];
    private List<INode> _allMemberNodes = [];

    public void SetAllMembers(List<INode> allmembers)
    {
        _allMemberNodes = allmembers;
    }

    public override INodeStateEvaluator GetEvaluator() => new GroupNodeStateEvaluator(this);
    public override bool ContainsEvent(object @event) => _allMemberNodes.Any(x => x.ContainsEvent(@event));
    public override bool ContainsNodeEvent(BpmEvent @event) => _allMemberNodes.Any(x => x.ContainsNodeEvent(@event));
}