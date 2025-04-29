using System;
using System.Collections.Generic;
using System.Linq;
using Core.BPM.Attributes;
using Core.BPM.Configuration;
using Core.BPM.Evaluators.Factory;
using Core.BPM.Exceptions;
using Core.BPM.Interfaces;

namespace Core.BPM.Nodes;

public class GuestProcessNode(Type guestProcessType, bool sealedSteps, Type processType, INodeEvaluatorFactory nodeEvaluatorFactory)
    : NodeBase(typeof(GuestProcessNode), processType, nodeEvaluatorFactory), INode
{
    public Type GuestProcessType { get; init; } = guestProcessType;
    public bool SealedSteps { get; init; } = sealedSteps;

    public override bool ContainsEvent(object @event)
    {
        var config = BProcessGraphConfiguration.GetConfig(GuestProcessType.Name);
        if (config is null)
            throw new NoDefinitionFoundException(GuestProcessType.Name);

        // Check if the event belongs to any node in this aggregate's process
        return config.RootNode.GetAllNodes().Any(node => node.ContainsEvent(@event));
    }

    public virtual bool ContainsEvent(List<object> events)
    {
        var config = BProcessGraphConfiguration.GetConfig(GuestProcessType.Name);
        if (config is null)
            throw new NoDefinitionFoundException(GuestProcessType.Name);

        // Check if the event belongs to any node in this aggregate's process
        var roots = config.RootNode.GetAllNodes();
        foreach (object @event in events)
        {
            if (roots.Any(node => node.ContainsEvent(@event)))
                return true;
        }

        return false;
    }

    public override bool ContainsNodeEvent(BpmEvent @event)
    {
        var config = BProcessGraphConfiguration.GetConfig(GuestProcessType.Name);
        if (config is null)
            throw new NoDefinitionFoundException(GuestProcessType.Name);

        // Check if the event is specifically associated with any node in this aggregate's process
        return config.RootNode.GetAllNodes().Any(node => node.ContainsNodeEvent(@event));
    }
}