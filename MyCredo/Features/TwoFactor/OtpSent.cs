using Core.BPM;
using Core.BPM.BCommand;

namespace MyCredo.Features.TwoFactor;

public record OtpSent(Guid ParentProcessId, string OtpHash) : BpmEvent;