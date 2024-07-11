using Core.BPM.BCommand;

namespace MyCredo.Features.Loan.OtpSend;
public record OtpSent(Guid OtpSessionId) : BpmEvent;
