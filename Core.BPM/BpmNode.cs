using Core.BPM.Interfaces;

namespace Core.BPM;

public class BpmNode<TProcess, TEvent> : INode<TProcess, TEvent> where TProcess : IProcess where TEvent : IEvent
{
    public BpmNode()
    {
        _eventType = typeof(TEvent);
    }

    public BpmNode(Type eventType)
    {
        _eventType = eventType;
    }

    private Type _eventType;

    public Predicate<TProcess>? Condition { get; set; }

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

    public INode? TraverseTo<TTraverseToEvent>() where TTraverseToEvent : IEvent
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

    public INode<TProcess, TEvent> AppendRight<TTEvent>(Predicate<TProcess>? expr = null)
        where TTEvent : IEvent
    {
        var newNode = new BpmNode<TProcess, TTEvent>() { Condition = expr };
        AddNextStep(newNode);
        newNode.AddPrevStep(this);
        return this;
    }

    public INode<TProcess, TTEvent> ThenAppendRight<TTEvent>(Predicate<TProcess>? expr = null)
        where TTEvent : IEvent
    {
        var newNode = new BpmNode<TProcess, TTEvent>()
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
}