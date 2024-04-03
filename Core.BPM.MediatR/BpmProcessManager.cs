using Core.BPM.Configuration;
using Core.BPM.MediatR.Exceptions;
using Core.BPM.MediatR.Mediator;
using Core.Persistence;

namespace Core.BPM.MediatR;

public class BpmProcessManager<TProcess> : IBpmValidationManager where TProcess : Aggregate
{
    private readonly MartenRepository<TProcess> _repo;

    public BpmProcessManager(MartenRepository<TProcess> repo)
    {
        _repo = repo;
    }

    public bool ValidateConfig<TCommand>()
    {
        var config = BProcessGraphConfiguration.GetConfig<TProcess>();
        if (config is null)
            throw BpmProcessNotConfiguredException.For<TProcess>(); //process not found exception

        var command = config.MoveTo<TCommand>();
        if (command is null)
            throw CommandNotConfiguredException.For<TProcess, TCommand>(); //graph command not found exception

        return true;
    }

    public async Task ValidateAsync<TCommand>(Guid documentId, CancellationToken cancellationToken)
    {
        var config = BProcessGraphConfiguration.GetConfig<TProcess>();
        if (config is null)
            throw BpmProcessNotConfiguredException.For<TProcess>(); //process not found exception

        var command = config.MoveTo<TCommand>();
        if (command is null)
            throw CommandNotConfiguredException.For<TProcess, TCommand>(); //graph command not found exception

        var agg = await _repo.GetWithEvents(documentId, cancellationToken: cancellationToken);
        var aggregate = agg.Item2;
        var events = agg.Item1.ToList();
        var latestEvent = events.Last().Data as CredoEvent;
        if (latestEvent is not null)
        {
            var latestProcessNode = config.RootNode.MoveTo(latestEvent.CommandType);
            if (latestProcessNode != null)
            {
                if (latestProcessNode.NextSteps.All(x => x.CommandType != typeof(TCommand)))
                    throw BpmWrongProcessPathException.For<TProcess, TCommand>(documentId,
                        latestProcessNode.NextSteps.Select(x => x.CommandType.ToString()).ToArray());
            }
        }

        var validCommandTryResult = command.ValidCommandTry(events.Select(x => x.EventType).ToList());
        if (!validCommandTryResult.Item2)
            throw BpmCommandNodeTryCoundReachedException.For<TProcess, TCommand>(validCommandTryResult
                .Item1); //node tryCount exception
    }

    public async Task StartProcess(TProcess aggregate, CancellationToken cancellationToken)
    {
        await _repo.Add(aggregate, cancellationToken);
    }

    public async Task<long> AppendEvent(Guid aggId, Action<TProcess> act, CancellationToken cancellationToken)
    {
        return await _repo.GetAndUpdate(aggId, act, ct: cancellationToken);
    }
}