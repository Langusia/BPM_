namespace Core.BPM.Persistence;

public interface IProcessRegistryRepository
{
    object AggregateStreamFromRegistry(Type aggregateType, IEnumerable<object> events);
    object? AggregateOrNullStreamFromRegistry(Type aggregateType, IEnumerable<object> events);
    object AggregateOrDefaultStreamFromRegistry(Type aggregateType, IEnumerable<object> events);
}