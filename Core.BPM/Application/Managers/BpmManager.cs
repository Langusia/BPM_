using Marten;
using Marten.Events;
using Core.BPM.Configuration;
using Core.BPM.Interfaces;
using Marten.Linq.MatchesSql;
using MediatR;

namespace Core.BPM.Application.Managers;

public class ProcessState<T> where T : Aggregate
{
    public ProcessState(T aggregate)
    {
        Aggregate = aggregate;
        ProcessConfig = BProcessGraphConfiguration.GetConfig<T>();
        CurrentStep = TraverseToEnd();
    }

    public ProcessState(T aggregate, BProcess config)
    {
        Aggregate = aggregate;
        ProcessConfig = config;
        CurrentStep = TraverseToEnd();
    }

    public T Aggregate { get; }
    public BProcess ProcessConfig { get; }
    public INode? CurrentStep { get; private set; }


    public bool ValidateFor<TCommand>() where TCommand : IBaseRequest
    {
        var latestValidNode = TraverseToEnd();
        if (latestValidNode is null)
            return false;
        if (latestValidNode!.NextSteps is null)
            return false;

        return latestValidNode.NextSteps.Any(x => x.CommandType == typeof(TCommand));
    }

    public void AppendEvent(Action<T> action)
    {
        action(Aggregate);
        CurrentStep = TraverseToEnd();
    }

    private INode? TraverseToEnd()
    {
        var persEventsCopy = new List<string>(Aggregate!.PersistedEvents);
        var currentStep = ProcessConfig.RootNode;

        if (persEventsCopy.Count == 0)
            return null;

        if (!ProcessConfig.RootNode.Validate(persEventsCopy))
            return null;

        if (persEventsCopy.Count == 0)
            return currentStep;

        var matched = true;
        while (matched && persEventsCopy.Count > 0)
        {
            matched = false;
            foreach (var step in currentStep.NextSteps!)
            {
                if (step.Validate(persEventsCopy))
                {
                    currentStep = step;
                    matched = true;
                    break;
                }

                matched = false;
            }

            if (!matched)
            {
                currentStep = null;
                break;
            }
        }

        return currentStep;
    }

    private INode? Find(Type command)
    {
        var persEventsCopy = new List<string>(Aggregate!.PersistedEvents);
        var currentStep = ProcessConfig.RootNode;
        INode? result;
        if (persEventsCopy.Count == 0)
            return null;

        if (!ProcessConfig.RootNode.Validate(persEventsCopy))
            return null;

        if (persEventsCopy.Count == 0)
            return currentStep;

        var matched = true;
        while (matched && persEventsCopy.Count > 0)
        {
            matched = false;
            foreach (var step in currentStep.NextSteps!)
            {
                if (step.Validate(persEventsCopy))
                {
                    if (command == step.CommandType)
                    {
                        result = step;
                        return result;
                    }

                    currentStep = step;
                    matched = true;
                    break;
                }

                matched = false;
            }

            if (!matched)
            {
                currentStep = null;
                break;
            }
        }

        return currentStep;
    }
}

public class BpmStore<T>(IDocumentSession session) where T : Aggregate
{
    private readonly IQuerySession _qSession = session;

    private T? _aggregate;
    private IReadOnlyList<IEvent> _persistedProcessEvents;
    private string _aggregateName;
    private BProcess? _config;
    private bool _newStream;

    public async Task<ProcessState<T>> StartProcess(Action<T> action, CancellationToken token)
    {
        action(_aggregate);
        _aggregate.Id = Guid.NewGuid();
        _aggregateName = typeof(T).Name;
        _config = BProcessGraphConfiguration.GetConfig(_aggregateName!);
        return new ProcessState<T>(_aggregate);
    }

    public async Task<BpmState> StartProcess(T aggregate, CancellationToken token)
    {
        aggregate.Id = Guid.NewGuid();
        _aggregate = aggregate;
        _aggregateName = aggregate.GetType().Name;
        _config = BProcessGraphConfiguration.GetConfig(_aggregateName!);
        session.SetHeader("AggregateType", aggregate.GetType().Name);
        var strAct = session.Events.StartStream<T>(aggregate.Id, aggregate.DequeueUncommittedEvents());
        _persistedProcessEvents = strAct.Events;
        await session.SaveChangesAsync(token: token).ConfigureAwait(false);
        var rootNode = BProcessGraphConfiguration.GetConfig(aggregate.GetType().Name)!.RootNode;
        return new BpmState { AggregateId = aggregate.Id, CurrentNode = rootNode, NextNodes = rootNode.NextSteps?.Select(x => x.CommandType.Name).ToList() };
    }

    public ProcessState<T> StartProcess(T aggregate)
    {
        aggregate.Id = Guid.NewGuid();
        _aggregate = aggregate;
        _aggregateName = aggregate.GetType().Name;
        _config = BProcessGraphConfiguration.GetConfig(_aggregateName!);
        _newStream = true;
        return new ProcessState<T>(_aggregate);
    }

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        session.SetHeader("AggregateType", _aggregateName);
        if (_newStream)
            session.Events.StartStream<T>(_aggregate.Id, _aggregate.DequeueUncommittedEvents());
        else
            session.Events.Append(_aggregate.Id, _aggregate.DequeueUncommittedEvents());

        await session.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<ProcessState<T>> AggregateProcessStateAsync(Guid aggregateId, CancellationToken ct)
    {
        _persistedProcessEvents = await _qSession.Events.FetchStreamAsync(aggregateId);
        var originalAggregateName = _persistedProcessEvents?.FirstOrDefault()?.Headers["AggregateType"].ToString();
        _aggregate = await _qSession.Events.AggregateStreamAsync<T>(aggregateId, token: ct);
        _config = BProcessGraphConfiguration.GetConfig(originalAggregateName);

        if (_aggregate is null)
            return null;

        if (_config is null)
            return null;

        return new ProcessState<T>(_aggregate!);
    }
}