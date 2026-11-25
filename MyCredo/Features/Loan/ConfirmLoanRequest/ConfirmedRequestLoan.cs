using BPM.Core.Attributes;
using BPM.Core.Events;
using MyCredo.Common;

namespace MyCredo.Features.Loan.ConfirmLoanRequest;

public record ConfirmedCarPawnshop(string TraceId, int UserId, ChannelTypeEnum Channel, int? TryCount) : BpmEvent;