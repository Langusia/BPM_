using Core.BPM.Configuration;
using Core.BPM.Interfaces;

namespace Core.BPM;

public class BpmNode<TProcess, TCommand> : INode<TProcess, TCommand> where TProcess : Aggregate
{
    public BpmNode()
    {
        _eventType = typeof(TCommand);
    }

    private readonly Type _eventType;
    private int? _permittedCommandTryCount;

    public List<INode<TProcess>> GetLastNodes()
    {
        throw new NotImplementedException();
    }

    public Predicate<TProcess>? Condition { get; set; }

    public bool ValidatePermittedCount(IList<IBpmEvent> events)
    {
        if (_permittedCommandTryCount is not null)
            if (events.Count(x => x.GetType() == typeof(TCommand)) >= _permittedCommandTryCount)
                throw new Exception();

        return true;
    }

    public void GetValidNodes(TProcess aggregate, List<INode<TProcess>> result)
    {
        if (Validate(aggregate))
            result.Add(this);

        foreach (var nextStep in NextSteps)
            (nextStep as INode<TProcess>)?.GetValidNodes(aggregate, result);
    }

    public bool Validate(TProcess aggregate) =>
        Condition is null || Condition.Invoke(aggregate);

    public Type CommandType => _eventType;

    public List<INode<TProcess>> NextSteps { get; set; }

    public void AddNextStep(INode<TProcess> node)
    {
        NextSteps ??= new List<INode<TProcess>>();
        NextSteps.Add(node);
    }

    public List<INode<TProcess>> PrevSteps { get; set; }

    public void AddPrevStep(INode<TProcess> node)
    {
        PrevSteps ??= new List<INode<TProcess>>();
        PrevSteps.Add(node);
    }

    public void SetConfig(NodeConfig<TProcess> cfg)
    {
        _permittedCommandTryCount = cfg.CommandTryCount;
    }

    public INode<TProcess, TTraverseToEvent>? MoveTo<TTraverseToEvent>() =>
        MoveTo(typeof(TTraverseToEvent)) as INode<TProcess, TTraverseToEvent>;

    public INode<TProcess>? MoveTo(Type eventType)
    {
        if (_eventType == eventType)
            return this;


        foreach (var nextStep in NextSteps)
        {
            var result = nextStep.MoveTo(eventType);
            if (result != null)
                return result;
        }

        return null;
    }

    public Tuple<int, bool> ValidCommandTry(IList<Type> events)
    {
        var currentCount = events?.Count(x => x == _eventType);
        return new Tuple<int, bool>(currentCount ?? 0, _permittedCommandTryCount is null ||
                                                       currentCount <= _permittedCommandTryCount);
    }

    public INode<TProcess, TCommand> Or<TCommandd>(Predicate<TProcess>? expr = null)
        //where TTEvent : IEvent
    {
        var newNode = new BpmNode<TProcess, TCommandd>() { Condition = expr };
        AddNextStep(newNode);
        newNode.AddPrevStep(this);
        return this;
    }

    public INode<TProcess, TCommand> Or<TNextCommand>(Action<INode<TProcess, TNextCommand>>? configure = null,
        Predicate<TProcess>? expr = null)
    {
        var newNode = new BpmNode<TProcess, TNextCommand>() { Condition = expr };
        configure?.Invoke(newNode);
        foreach (var prevStep in PrevSteps)
        {
            prevStep.AddNextStep(newNode);
        }

        newNode.AddPrevStep(this);
        return this;
    }

    public INode<TProcess, TNextCommand> ThenAppendRight<TNextCommand>(Predicate<TProcess>? expr = null)
        //where TTEvent : IEvent
    {
        var newNode = new BpmNode<TProcess, TNextCommand>() { Condition = expr };
        AddNextStep(newNode);
        newNode.AddPrevStep(this);

        return newNode;
    }

    public INode<TProcess, TNextCommand> ContinueWith<TNextCommand>(
            Action<INode<TProcess, TNextCommand>>? configure = null, Predicate<TProcess>? expr = null)
        //where TTEvent : IEvent
    {
        var newNode = new BpmNode<TProcess, TNextCommand>()
        {
            Condition = expr,
        };

        newNode.AddPrevStep(this);
        if (NextSteps is null)
            NextSteps = new List<INode<TProcess>>();

        AddNextStep(newNode);

        //foreach (var nextStep in NextSteps)
        //{
        //    nextStep.AddNextStep(newNode);
        //}

        if (configure is not null)
            configure.Invoke(newNode);

        return newNode;
    }
}