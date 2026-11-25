using BPM.Core.Attributes;
using BPM.Core.Events;

namespace MyCredo.Features.Loan.OtpSend;
public record OtpSent(Guid OtpSessionId) : BpmEvent;
