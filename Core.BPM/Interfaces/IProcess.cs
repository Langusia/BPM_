namespace Core.BPM.Interfaces;

public interface IAggregate
{
    Guid Id { get; }
    int Version { get; }

    object[] DequeueUncommittedEvents();
}