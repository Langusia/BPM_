using System;

namespace Core.BPM.Definition.Conditions;

public interface IAggregateCondition
{
    Type ConditionalAggregateType { get; init; }
    bool EvaluateAggregateCondition(object aggregate);
}
