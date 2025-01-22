using Core.BPM.Persistence;

namespace Core.BPM.Application.Managers;

public class BpmStore(IBpmRepository repository) : IBpmStore
{
    private readonly Queue<IProcess> _processes = [];

    public IProcess StartProcess<T>(params object[] events) where T : Aggregate
    {
        return StartProcess(typeof(T), events);
    }

    public IProcess StartProcess(Type aggregateType, params object[] events)
    {
        var process = new Process(Guid.NewGuid(), aggregateType.Name, true, null, events, repository);
        _processes.Enqueue(process);
        return process;
    }

    public async Task<IProcess> FetchProcessAsync(Guid aggregateId, CancellationToken ct)
    {
        var stream = await repository.FetchStreamAsync(aggregateId, ct);
        var aggregateName = stream.FirstOrDefault()?.Headers?["AggregateType"].ToString();
        if (string.IsNullOrEmpty(aggregateName))
            throw new Exception();

        var process = new Process(aggregateId, aggregateName, false, stream.Select(x => x.Data).ToArray(), null, repository);
        _processes.Enqueue(process);
        return process;
    }

    public async Task SaveChangesAsync(CancellationToken token)
    {
        foreach (var process in _processes)
        {
            await ((IProcessStore)process).AppendUncommittedToDb(token);
        }

        await repository.SaveChangesAsync(token);
    }
}