using Core.BPM;
using Core.BPM.Attributes;

namespace MyCredo.Features.TwoFactor;

public record OtpSubmited(string OtpHash, bool IsValid) : BpmEvent;