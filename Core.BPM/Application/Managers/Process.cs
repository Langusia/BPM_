using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Core.BPM.Application.Events;
using Core.BPM.Attributes;
using Core.BPM.Configuration;
using Core.BPM.Interfaces;
using Core.BPM.Persistence;
using MediatR;

namespace Core.BPM.Application.Managers;

public class Process : IProcess, IProcessStore
{
    public Guid Id { get; }
    public string AggregateName { get; }
    public List<object> StoredEvents { get; } = [];
    public Queue<object> UncommittedEvents { get; } = [];
    public List<INode>? AvailableSteps { get; private set; }
    public DateTimeOffset? StartTime { get; }

    private readonly BProcess _processConfig;
    private readonly IBpmRepository _repository;
    private bool _isNewProcess;

    public Process(Guid aggregateId, string rootAggregateName, bool isNewProcess, IEnumerable<object>? events, IEnumerable<object>? upcomingEvents, DateTimeOffset? startTime,
        List<INode>? availableSteps, IBpmRepository repository)
    {
        StartTime = startTime;
        AvailableSteps = availableSteps;
        _processConfig = BProcessGraphConfiguration.GetConfig(rootAggregateName) ?? throw new Exception();
        if (events is not null)
            StoredEvents = events.ToList();
        if (upcomingEvents != null)
            foreach (var upcomingEvent in upcomingEvents)
            {
                UncommittedEvents.Enqueue(upcomingEvent);
            }

        Id = aggregateId;
        AggregateName = rootAggregateName;
        _isNewProcess = isNewProcess;

        _repository = repository;
    }


    public T AggregateAs<T>(bool includeUncommitted = true) where T : Aggregate
    {
        var stream = StoredEvents;
        if (includeUncommitted)
            stream = MergedWithUncommitted();

        var aggregate = (T)_repository.AggregateStreamFromRegistry(typeof(T), stream);
        aggregate.Id = Id;
        return aggregate;
    }

    public T? AggregateOrNullAs<T>(bool includeUncommitted = true) where T : Aggregate
    {
        try
        {
            var stream = StoredEvents;
            if (includeUncommitted)
                stream = MergedWithUncommitted();

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
            var stream = StoredEvents;
            if (includeUncommitted)
                stream = MergedWithUncommitted();

            aggregate = (T)_repository.AggregateStreamFromRegistry(typeof(T), stream);
            aggregate.Id = Id;
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private List<object> MergedWithUncommitted() =>
        StoredEvents.Union(UncommittedEvents.ToList()).ToList();


    public BpmResult AppendEvent(BpmEvent @event)
    {
        if (CheckExpiration(out var bpmResult))
            return bpmResult;

        var stream = MergedWithUncommitted();
        if (CheckFail(stream, out var failResult))
            return failResult;
        var availableNodesResult = _processConfig.RootNode.GetCheckBranchCompletionAndGetAvailableNodesFromCache(stream);
        var eventNodes = availableNodesResult.availableNodes.Where(x => x.ContainsEvent(@event)).ToList();
        if (eventNodes is null || eventNodes.Count == 0)
            return Result.Fail(Code.InvalidEvent);

        foreach (var eventNode in eventNodes)
        {
            @event.NodeId = eventNode.NodeLevel;
            UncommittedEvents.Enqueue(@event);
        }


        AvailableSteps = availableNodesResult.availableNodes;
        return Result.Success();
    }

    public BpmResult ForceAppendEvents(params object[] events)
    {
        foreach (var @event in events)
            UncommittedEvents.Enqueue(@event);

        return Result.Success();
    }


    private bool CheckExpiration(out BpmResult bpmResult)
    {
        bpmResult = Result.Success();
        if (StartTime is not null)
            if (_processConfig.Config.ExpirationSeconds is not null)
                if ((DateTimeOffset.UtcNow - StartTime).Value.TotalSeconds > _processConfig.Config.ExpirationSeconds)
                {
                    bpmResult = Result.Fail(Code.Expired);
                    return true;
                }

        return false;
    }

    private bool CheckFail(List<object> stream, out BpmResult bpmResult)
    {
        bpmResult = Result.Success();
        if (stream.Any(x => x is ProcessFailed))
        {
            bpmResult = Result.Fail(Code.ProcessFailed);
            return true;
        }

        return false;
    }

    public BpmResult AppendFail<T>(string description, object data)
    {
        if (CheckExpiration(out var bpmResult))
            return bpmResult;

        UncommittedEvents.Enqueue(new ProcessFailed(typeof(T).Name, data, description));
        return Result.Success();
    }

    public void ClearUncommittedEvents()
    {
        UncommittedEvents.Clear();
    }

    public BpmResult Validate<T>(bool includeUncommitted) where T : IBaseRequest
    {
        if (CheckExpiration(out var bpmResult))
            return bpmResult;

        var stream = StoredEvents;
        if (includeUncommitted)
            stream = MergedWithUncommitted();
        if (CheckFail(stream, out var failResult))
            return failResult;

        var filteredResult = _processConfig.RootNode.GetCheckBranchCompletionAndGetAvailableNodesFromCache(stream);
        if (filteredResult.availableNodes.All(x => x.CommandType != typeof(T)))
            return Result.Fail(Code.InvalidEvent);

        AvailableSteps = filteredResult.availableNodes;

        return Result.Success();
    }

    public BpmResult<List<INode>?> GetNextSteps(bool includeUnsavedEvents = true)
    {
        var stream = StoredEvents;
        if (includeUnsavedEvents)
            stream = MergedWithUncommitted();

        var res = new List<(string, INode, bool isCompleted, bool canExec, List<INode> availableNodes)>();
        var result = _processConfig.RootNode.GetCheckBranchCompletionAndGetAvailableNodesFromCache(stream, res);
        AvailableSteps = result.availableNodes;
        return Result.Success(result.availableNodes.Distinct().ToList());
    }


    public async Task AppendUncommittedToDb(CancellationToken ct)
    {
        var headers = new Dictionary<string, object> { { "AggregateType", AggregateName } };
        await _repository.AppendEvents(Id, UncommittedEvents.ToArray(), _isNewProcess, headers, ct);
        StoredEvents.AddRange(UncommittedEvents.ToArray());
        UncommittedEvents.Clear();
        _isNewProcess = false;
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        await _repository.SaveChangesAsync(ct);
    }
}