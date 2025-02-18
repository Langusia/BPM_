using Core.BPM.Application.Events;
using Core.BPM.Configuration;
using Core.BPM.Interfaces;
using Core.BPM.Persistence;
using MediatR;

namespace Core.BPM.Application.Managers;

public class Process : IProcess, IProcessStore
{
    public Guid Id { get; }
    public string AggregateName { get; }

    private readonly DateTimeOffset? _startTime;
    private readonly BProcess _processConfig;
    private readonly IBpmRepository _repository;

    private readonly List<object> _storedEvents = [];
    private readonly Queue<object> _uncommittedEvents = [];

    private bool _isNewProcess;

    public Process(Guid aggregateId, string rootAggregateName, bool isNewProcess, IEnumerable<object>? events, IEnumerable<object>? upcomingEvents, DateTimeOffset? startTime,
        IBpmRepository repository)
    {
        _startTime = startTime;
        _processConfig = BProcessGraphConfiguration.GetConfig(rootAggregateName) ?? throw new Exception();
        if (events is not null)
            _storedEvents = events.ToList();
        if (upcomingEvents != null)
            foreach (var upcomingEvent in upcomingEvents)
            {
                _uncommittedEvents.Enqueue(upcomingEvent);
            }

        Id = aggregateId;
        AggregateName = rootAggregateName;
        _isNewProcess = isNewProcess;

        _repository = repository;
    }


    public T AggregateAs<T>(bool includeUncommitted = true) where T : Aggregate
    {
        var stream = _storedEvents;
        if (includeUncommitted)
            stream = stream.Union(_uncommittedEvents).ToList();

        var aggregate = (T)_repository.AggregateStreamFromRegistry(typeof(T), stream);
        aggregate.Id = Id;
        return aggregate;
    }

    public T? AggregateOrNullAs<T>(bool includeUncommitted = true) where T : Aggregate
    {
        try
        {
            var stream = _storedEvents;
            if (includeUncommitted)
                stream = stream.Union(_uncommittedEvents).ToList();

            var aggregate = (T)_repository.AggregateStreamFromRegistry(typeof(T), stream);
            aggregate.Id = Id;
            return aggregate;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public bool TryAggregateAs<T>(out T? aggregate, bool includeUncommitted = true) where T : Aggregate
    {
        aggregate = null;
        try
        {
            var stream = _storedEvents;
            if (includeUncommitted)
                stream = stream.Union(_uncommittedEvents).ToList();

            aggregate = (T)_repository.AggregateStreamFromRegistry(typeof(T), stream);
            aggregate.Id = Id;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }


    public BpmResult AppendEvents(params object[] events)
    {
        if (CheckExpiration(out var bpmResult))
            return bpmResult;

        var stream = _storedEvents;
        stream = stream.Union(_uncommittedEvents).ToList();

        var filteredResult = _processConfig.RootNode.GetCheckBranchCompletionAndGetAvailableNodesFromCache(stream);
        if (filteredResult.availableNodes.All(x => events.All(z => !x.ContainsEvent(z))))
            return Result.Fail(Code.InvalidEvent);

        foreach (var @event in events)
        {
            _uncommittedEvents.Enqueue(@event);
        }

        return Result.Success();
    }

    public BpmResult ForceAppendEvents(params object[] events)
    {
        foreach (var @event in events)
            _uncommittedEvents.Enqueue(@event);

        return Result.Success();
    }


    private bool CheckExpiration(out BpmResult bpmResult)
    {
        bpmResult = Result.Success();
        if (_startTime is not null)
            if (_processConfig.Config.ExpirationSeconds is not null)
                if ((DateTimeOffset.UtcNow - _startTime).Value.TotalSeconds > _processConfig.Config.ExpirationSeconds)
                {
                    bpmResult = Result.Fail(Code.Expired);
                    return true;
                }

        return false;
    }

    public BpmResult AppendFail<T>(string description, object data)
    {
        if (CheckExpiration(out var bpmResult))
            return bpmResult;

        _uncommittedEvents.Enqueue(new ProcessFailed(typeof(T).Name, data, description));
        return Result.Success();
    }

    public BpmResult Validate<T>(bool includeUncommitted) where T : IBaseRequest
    {
        if (CheckExpiration(out var bpmResult))
            return bpmResult;

        var stream = _storedEvents;
        if (includeUncommitted)
            stream = stream.Union(_uncommittedEvents).ToList();


        var filteredResult = _processConfig.RootNode.GetCheckBranchCompletionAndGetAvailableNodesFromCache(stream);
        if (filteredResult.availableNodes.All(x => x.CommandType != typeof(T)))
            return Result.Fail(Code.InvalidEvent);

        return Result.Success();
    }

    public BpmResult<List<INode>?> GetNextSteps(bool includeUnsavedEvents = true)
    {
        var stream = _storedEvents;
        if (includeUnsavedEvents)
            stream = stream.Union(_uncommittedEvents).ToList();

        var result = _processConfig.RootNode.GetCheckBranchCompletionAndGetAvailableNodesFromCache(stream);
        return Result.Success(result.availableNodes.Distinct().ToList());
    }


    public async Task AppendUncommittedToDb(CancellationToken ct)
    {
        var headers = new Dictionary<string, object> { { "AggregateType", AggregateName } };
        await _repository.AppendEvents(Id, _uncommittedEvents.ToArray(), _isNewProcess, headers, ct);
        _isNewProcess = false;
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await _repository.SaveChangesAsync(ct);
    }
}