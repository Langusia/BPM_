using BPM.Core;
using BPM.Core.Process;
using BPM.Core.Events;
using BPM.Core.Attributes;

namespace MyCredo.Features.TwoFactor;

public record OtpSent(Guid ParentProcessId, string OtpHash) : BpmEvent;