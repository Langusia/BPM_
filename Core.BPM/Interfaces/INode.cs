namespace Core.BPM.Interfaces;

public interface INode<TProcess, TCommand> : INode<TProcess>
    where TProcess : IProcess
{
    INode<TProcess>? AvaiableNodes();

    //Predicate<TProcess>? Condition { get; set; }
    //INode<TProcess, TCommand> AppendRight(Predicate<TProcess>? expr = null);
    ////where TTEvent : IEvent;
    //
    //INode<TProcess, TCommand> ThenAppendRight(Predicate<TProcess>? expr = null);
    //where TTEvent : IEvent;
}

public interface INode<TProcess> : INode
    where TProcess : IProcess
{
    Predicate<TProcess>? Condition { get; set; }

    bool Valid(TProcess aggregate) => Condition != null && Condition.Invoke(aggregate);
}

public interface INode
{
    Type EventType { get; }
    List<INode> NextSteps { get; set; }
    void AddNextStep(INode node);
    List<INode> PrevSteps { get; set; }
    void AddPrevStep(INode node);

    INode? TraverseTo<TEvent>();
    INode? TraverseTo(Type eventType);
}