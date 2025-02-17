using Core.BPM.Attributes;
using MyCredo.Common;

namespace MyCredo.Features.Loan.ConfirmLoanRequest;

public record ConfirmedCarPawnshop(string TraceId, int UserId, ChannelTypeEnum Channel, int? TryCount) : BpmEvent;