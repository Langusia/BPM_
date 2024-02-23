namespace Core.BPM.Interfaces;

public interface IProcess
{
    Guid Id { get; }
    int Version { get; }

    object[] DequeueUncommittedEvents();
}