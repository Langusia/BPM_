namespace Core.BPM.AggregateConditions;

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

public class AggregateConditionWrapper()
{
    private List<IAggregateCondition> _aggregateConditions;

    public void AddCondition<T>(Predicate<T> aggregateCondition) where T : Aggregate
    {
        _aggregateConditions.Add(new AggregateCondition<T>(aggregateCondition));
    }

    public bool EvaluateAllAggregatePredicates(object aggregate)
    {
        foreach (var aggregateCondition in _aggregateConditions)
        {
            aggregateCondition.EvaluateAggregateCondition(null);
        }

        throw new Exception();
    }
}