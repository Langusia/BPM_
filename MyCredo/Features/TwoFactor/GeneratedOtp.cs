using Core.BPM;
using Core.BPM.BCommand;

namespace MyCredo.Features.TwoFactor;

public record GeneratedOtp(Guid ParentProcessId, string OtpHash) : BpmEvent;