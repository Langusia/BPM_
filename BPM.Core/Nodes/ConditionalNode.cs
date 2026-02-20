using System;
using System.Collections.Generic;
using System.Linq;
using BPM.Core.Definition.Conditions;
using BPM.Core.Events;
using BPM.Core.Nodes.Evaluation;

namespace BPM.Core.Nodes;

public class ConditionalNode(Type processType, IAggregateCondition aggregateCondition, INodeEvaluatorFactory nodeEvaluatorFactory)
    : NodeBase(typeof(ConditionalNode), processType, nodeEvaluatorFactory)
{
    public IAggregateCondition AggregateCondition = aggregateCondition;

    public List<INode> IfNodeRoots { get; set; }
    public List<INode>? ElseNodeRoots { get; set; }

    public override bool ContainsEvent(object @event) => IfNodeRoots.SelectMany(x => x.GetAllNodes())
        .Union(ElseNodeRoots?.SelectMany(x => x.GetAllNodes()) ?? Enumerable.Empty<INode>()).Any(x => x.ContainsEvent(@event));

    public override bool ContainsNodeEvent(BpmEvent @event) => IfNodeRoots.SelectMany(x => x.GetAllNodes())
        .Union(ElseNodeRoots?.SelectMany(x => x.GetAllNodes()) ?? Enumerable.Empty<INode>()).Any(x => x.ContainsNodeEvent(@event));
}
