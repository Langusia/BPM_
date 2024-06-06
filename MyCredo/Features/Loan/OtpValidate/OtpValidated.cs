using Core.BPM.BCommand;

namespace MyCredo.Retail.Loan.Application.Features.TwoFactor.OtpValidate;
public record OtpValidated(Guid OtpSessionId, bool ValidOtp) : BpmEvent;
