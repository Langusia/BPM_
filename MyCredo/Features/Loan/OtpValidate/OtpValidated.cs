using Core.BPM.BCommand;

namespace MyCredo.Features.Loan.OtpValidate;
public record OtpValidated(Guid OtpSessionId, bool ValidOtp) : BpmEvent;
