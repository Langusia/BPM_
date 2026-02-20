using System;
using BPM.Core.Process;

namespace BPM.Core.Definition.Conditions;

public class AggregateCondition<TAggregate>(Predicate<TAggregate> aggregatePredicate) : IAggregateCondition where TAggregate : Aggregate
{
    public Type ConditionalAggregateType { get; init; } = typeof(TAggregate);

    public bool EvaluateAggregateCondition(object aggregate)
    {
        if (aggregate is TAggregate ctx)
            return aggregatePredicate(ctx);

        throw new Exception();
    }
}
