using BPM.Core.Attributes;
using BPM.Core.Events;

namespace MyCredo.Features.Loan.OtpValidate;
public record OtpValidated(Guid OtpSessionId, bool ValidOtp) : BpmEvent;
