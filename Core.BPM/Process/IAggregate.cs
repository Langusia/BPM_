using System;

namespace Core.BPM.Process;

public interface IAggregate
{
    Guid Id { get; }
    int Version { get; }
    bool? IsCompleted();
    object[] DequeueUncommittedEvents();
}