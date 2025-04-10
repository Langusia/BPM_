using System;
using System.Threading;
using System.Threading.Tasks;
using Core.BPM.Attributes;

namespace Core.BPM.Application.Managers;

public interface IBpmStore
{
    IProcess? StartProcess<T>(BpmEvent events) where T : Aggregate;
    IProcess? StartProcess<T>() where T : Aggregate;
    IProcess? StartProcess(Type aggregateType, BpmEvent events);
    Task<IProcess> FetchProcessAsync(Guid aggregateId, CancellationToken ct);

    Task SaveChangesAsync(CancellationToken token);
}