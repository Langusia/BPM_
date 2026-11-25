using BPM.Core;
using BPM.Core.Process;
using BPM.Core.Attributes;
using BPM.Core.Events;

namespace MyCredo.Features.TwoFactor;

public record OtpSubmited(string OtpHash, bool IsValid) : BpmEvent;