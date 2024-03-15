using Core.BPM.Configuration;

namespace Core.BPM.Interfaces;

public interface INode<TProcess, TCommand> : INode<TProcess>
    where TProcess : IAggregate
{
    //int? PermittedCommandTryCount { get; set; }

    INode<TProcess, TCommand> Or<TNextCommand>(Action<INode<TProcess, TNextCommand>>? configure = null,
        Predicate<TProcess>? expr = null);

    INode<TProcess, TNextCommand> ThenAppendRight<TNextCommand>(Predicate<TProcess>? expr = null);

    INode<TProcess, TNextCommand> ContinueWith<TNextCommand>(
        Action<INode<TProcess, TNextCommand>>? configure = null, Predicate<TProcess>? expr = null);

    void SetConfig(NodeConfig<TProcess> cfg);

    INode<TProcess, TEvent>? MoveTo<TEvent>();
}

public interface INode<TProcess> : INode
    where TProcess : IAggregate
{
    Predicate<TProcess>? Condition { get; set; }
    void GetValidNodes(TProcess aggregate, List<INode<TProcess>> result);
    bool Validate(TProcess aggregate);

    List<INode<TProcess>> NextSteps { get; set; }
    void AddNextStep(INode<TProcess> node);
    List<INode<TProcess>> PrevSteps { get; set; }
    void AddPrevStep(INode<TProcess> node);


    INode<TProcess>? MoveTo(Type eventType);
    Tuple<int, bool> ValidCommandTry(IList<Type> events);
}

public interface INode
{
    Type CommandType { get; }
}