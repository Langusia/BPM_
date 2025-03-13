using Core.BPM.Attributes;

namespace Core.BPM.Application.Managers;

public interface IBpmStore
{
    IProcess? StartProcess<T>(BpmEvent events) where T : Aggregate;
    IProcess? StartProcess(Type aggregateType, BpmEvent events);
    Task<IProcess> FetchProcessAsync(Guid aggregateId, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken token);
}