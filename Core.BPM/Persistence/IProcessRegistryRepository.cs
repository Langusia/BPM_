using System;
using System.Collections.Generic;

namespace Core.BPM.Persistence;

public interface IProcessRegistryRepository
{
    object AggregateStreamFromRegistry(Type aggregateType, IEnumerable<object> events);
    object? AggregateOrNullStreamFromRegistry(Type aggregateType, IEnumerable<object> events);
    object AggregateOrDefaultStreamFromRegistry(Type aggregateType, IEnumerable<object> events);
    bool TryAggregateAs<T>(out T? aggregate, IEnumerable<object> events) where T : Aggregate;
    public bool TryAggregateAs(Type aggType, IEnumerable<object> stream, out Aggregate? aggregate);
}