namespace Core.BPM.Persistence;

public interface IProcessRegistryRepository
{
    object AggregateStreamFromRegistry(Type aggregateType, IEnumerable<object> events);
}