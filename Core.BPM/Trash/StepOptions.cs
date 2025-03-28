﻿using Core.BPM.Configuration;

namespace Core.BPM.Trash;

public interface IStepOptions
{
    bool EvaluateAggregateCondition(object aggregate);
}

public class StepOptions : IStepOptions
{
    public StepOptions(Type aggregateType, Type commandType)
    {
        AggregateType = aggregateType;
        CommandType = commandType;
        ProducingEvents = BProcessGraphConfiguration.GetCommandProducer(commandType).EventTypes.Select(x => x.Name).ToList();
    }

    public Type AggregateType { get; set; }
    public Type CommandType { get; set; }
    public List<string> ProducingEvents { get; set; }

    public int? MaxCount { get; set; }
    public string Alias { get; set; }

    public virtual bool EvaluateAggregateCondition(object aggregate)
    {
        return true;
    }
}

public class StepOptions<TAggregate>(Type aggregateType, Type commandType) : StepOptions(aggregateType, commandType)
    where TAggregate : Aggregate
{
    public Predicate<TAggregate>? AggregateCondition { get; set; }

    public override bool EvaluateAggregateCondition(object aggregate)
    {
        if (aggregate is TAggregate typedAggregate)
        {
            return AggregateCondition?.Invoke(typedAggregate) ?? true;
        }

        throw new ArgumentException($"Invalid aggregate type. Expected {typeof(TAggregate).Name}, got {aggregate.GetType().Name}.");
    }
}

public record BpmProcessEventOptions
{
    public string ProcessName { get; set; }
    public List<StepOptions> BpmCommandtOptions { get; set; }
}