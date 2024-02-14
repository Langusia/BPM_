namespace Core.BPM.Interfaces;

public interface INode<TProcess, TEvent> : INode
    where TEvent : IEvent
    where TProcess : IProcess
{
    INode<TProcess, TEvent> AppendRight<TTEvent>(Predicate<TProcess>? expr = null)
        where TTEvent : IEvent;

    INode<TProcess, TTEvent> ThenAppendRight<TTEvent>(Predicate<TProcess>? expr = null)
        where TTEvent : IEvent;
}

public interface INode
{
    Type EventType { get; }
    List<INode> NextSteps { get; set; }
    void AddNextStep(INode node);
    List<INode> PrevSteps { get; set; }
    void AddPrevStep(INode node);

    INode? TraverseTo<TEvent>() where TEvent : IEvent;
    INode? TraverseTo(Type eventType);
}