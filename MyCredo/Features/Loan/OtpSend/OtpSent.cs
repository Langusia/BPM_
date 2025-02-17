using Core.BPM.Attributes;

namespace MyCredo.Features.Loan.OtpSend;
public record OtpSent(Guid OtpSessionId) : BpmEvent;
