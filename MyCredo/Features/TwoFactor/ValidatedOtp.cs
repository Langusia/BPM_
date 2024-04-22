using Core.BPM;
using Core.BPM.BCommand;

namespace MyCredo.Features.TwoFactor;

public record OtpSubmited(string OtpHash, bool IsValid) : BpmEvent;