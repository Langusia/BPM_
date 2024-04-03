namespace Core.BPM.Interfaces;

public interface IProcess<TProcess> where TProcess : IAggregate
{
    List<INode> GetConditionValidGraphNodes(TProcess aggregate);
}

public interface IAggregate
{
    Guid Id { get; }
    int Version { get; }

    object[] DequeueUncommittedEvents();
}