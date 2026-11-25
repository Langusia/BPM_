// Kept in the original BPM.Core.Events namespace: this base record moved from
// BPM.Core so event contracts can compile against BPM.Contracts alone, while
// existing source that references BPM.Core.Events keeps compiling unchanged.
namespace BPM.Core.Events;

public abstract record BpmEvent
{
    public int NodeId { get; set; }
}
