using Core.BPM.BCommand;

namespace MyCredo.Retail.Loan.Application.Features.TwoFactor.OtpSend;
public record OtpSent(Guid OtpSessionId) : BpmEvent;
