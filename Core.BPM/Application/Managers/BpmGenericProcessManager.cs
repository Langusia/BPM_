using Core.BPM.Persistence;
using Core.Persistence;

namespace Core.BPM.MediatR.Managers;

public class BpmGenericProcessManager<TProcess> where TProcess : Aggregate
{
    private readonly MartenRepository<TProcess> _repo;

    public BpmGenericProcessManager(MartenRepository<TProcess> repo)
    {
        _repo = repo;
    }

    public async Task StartProcess(TProcess aggregate, CancellationToken cancellationToken)
    {
        await _repo.Add(aggregate, cancellationToken);
    }
}