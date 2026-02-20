using System;

namespace BPM.Core.Definition.Conditions;

public interface IAggregateCondition
{
    Type ConditionalAggregateType { get; init; }
    bool EvaluateAggregateCondition(object aggregate);
}
