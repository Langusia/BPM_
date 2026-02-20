using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BPM.Core.Events;
using BPM.Core.Configuration;
using BPM.Core.Persistence;

namespace BPM.Core.Process;

public class ProcessStore(IBpmRepository repository) : IProcessStore
{
    private readonly Queue<IProcess> _processes = [];

    public IProcess? StartProcess<T>() where T : Aggregate =>
        new Process(Guid.NewGuid(), typeof(T).Name, true, null, null, null, null, repository);

    public IProcess? StartProcess<T>(BpmEvent @event) where T : Aggregate => StartProcess(typeof(T), @event);

    public IProcess? StartProcess(Type aggregateType, BpmEvent @event)
    {
        var config = BProcessGraphConfiguration.GetConfig(aggregateType.Name)!;
        if (!config.RootNode.ContainsEvent(@event))
            return null;

        @event.NodeId = config.RootNode.NodeLevel;
        var process = new Process(Guid.NewGuid(), aggregateType.Name, true, null, [@event], null,
            config.RootNode.NextSteps, repository);
        _processes.Enqueue(process);
        return process;
    }

    public async Task<IProcess> FetchProcessAsync(Guid aggregateId, CancellationToken ct)
    {
        var stream = await repository.FetchStreamAsync(aggregateId, ct);
        var aggregateName = stream.FirstOrDefault()?.Headers?["AggregateType"].ToString();
        if (string.IsNullOrEmpty(aggregateName))
            throw new Exception();

        var process = new Process(aggregateId, aggregateName, false, stream.Select(x => x.Data).ToArray(), null, stream.FirstOrDefault().Timestamp, null, repository);
        _processes.Enqueue(process);
        return process;
    }

    public async Task SaveChangesAsync(CancellationToken token)
    {
        foreach (var process in _processes)
        {
            await ((Process)process).AppendUncommittedToDb(token);
        }

        await repository.SaveChangesAsync(token);
    }
}
