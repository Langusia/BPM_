using System;

namespace Core.BPM.AggregateConditions;

public interface IAggregateCondition
{
    Type ConditionalAggregateType { get; init; }
    bool EvaluateAggregateCondition(object aggregate);
}