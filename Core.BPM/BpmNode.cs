using Core.BPM.Interfaces;

namespace Core.BPM;

public class BpmNode<TProcess, TCommand> : INode<TProcess, TCommand> where TProcess : IProcess
{
    public BpmNode()
    {
        _eventType = typeof(TCommand);
    }

    public BpmNode(Type eventType)
    {
        _eventType = eventType;
    }

    private Type _eventType;

    public Predicate<TProcess>? Condition { get; set; }

    public INode<TProcess>? AvaiableNodes()
    {
        foreach (var nextStep in NextSteps)
        {
            nextStep.
        }
    }

    public Type EventType => _eventType;

    public List<INode> NextSteps { get; set; }

    public void AddNextStep(INode node)
    {
        NextSteps ??= new List<INode>();
        NextSteps.Add(node);
    }

    public List<INode> PrevSteps { get; set; }

    public void AddPrevStep(INode node)
    {
        PrevSteps ??= new List<INode>();
        PrevSteps.Add(node);
    }

    public INode? TraverseTo<TTraverseToEvent>()
    {
        if (_eventType == typeof(TTraverseToEvent))
            return this;

        foreach (var nextStep in NextSteps)
        {
            var result = nextStep.TraverseTo<TTraverseToEvent>();
            if (result != null)
                return result;
        }

        return null;
    }

    public INode? TraverseTo(Type eventType)
    {
        if (_eventType == eventType)
            return this;


        foreach (var nextStep in NextSteps)
        {
            var result = nextStep.TraverseTo(eventType);
            if (result != null)
                return result;
        }

        return null;
    }

    public INode<TProcess, TCommand> AppendRight<TCommandd>(Predicate<TProcess>? expr = null)
        //where TTEvent : IEvent
    {
        var newNode = new BpmNode<TProcess, TCommandd>() { Condition = expr };
        AddNextStep(newNode);
        newNode.AddPrevStep(this);
        return this;
    }

    public INode<TProcess, TCommandd> ThenAppendRight<TCommandd>(Predicate<TProcess>? expr = null)
        //where TTEvent : IEvent
    {
        var newNode = new BpmNode<TProcess, TCommandd>()
        {
            Condition = expr,
        };

        newNode.AddPrevStep(this);
        foreach (var nextStep in NextSteps)
        {
            nextStep.AddNextStep(newNode);
        }

        return newNode;
    }

    public void SetCondition<T>(Predicate<T> condition)
    {
        throw new NotImplementedException();
    }

    public Predicate<T> GetCondition<T>()
    {
        throw new NotImplementedException();
    }
}