namespace Core.BPM.Application.Managers;

public interface IBpmStore
{
    IProcess? StartProcess<T>(object events) where T : Aggregate;
    IProcess? StartProcess(Type aggregateType, object events);
    Task<IProcess> FetchProcessAsync(Guid aggregateId, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken token);
}