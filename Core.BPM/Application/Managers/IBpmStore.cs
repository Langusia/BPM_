namespace Core.BPM.Application.Managers;

public interface IBpmStore
{
    IProcess StartProcess<T>(params object[] events) where T : Aggregate;
    IProcess StartProcess(Type aggregateType, params object[] events);
    Task<IProcess> FetchProcessAsync(Guid aggregateId, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken token);
}