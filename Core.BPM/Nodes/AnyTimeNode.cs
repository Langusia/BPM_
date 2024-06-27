﻿using Core.BPM.Interfaces;
using MediatR;

namespace Core.BPM.Nodes;

public class AnyTimeNode(Type commandType, Type processType) : NodeBase(commandType, processType), INode
{
    public bool Validate(List<string> events)
    {
        var @event = events.FirstOrDefault();
        var matched = GetCommandProducer(CommandType).EventTypes.Any(x => x.Name == @event);
        if (matched)
            events.RemoveAll(x => x == @event);

        return matched;
    }

    public void AddNextStepToTail(INode node)
    {
        if (NextSteps.Count == 0)
            NextSteps.Add(node);
        else
        {
            var tails = new List<INode>();
            GetLastNodes(tails, this);
            tails.Distinct().ToList().ForEach(x => x.AddNextStep(node));
        }
    }
}