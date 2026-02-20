using System;

namespace BPM.Core.Process;

public interface IAggregate
{
    Guid Id { get; }
    int Version { get; }
    bool? IsCompleted();
    object[] DequeueUncommittedEvents();
}