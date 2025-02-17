using Core.BPM.Attributes;
using MediatR;
using MyCredo.Common;

namespace MyCredo.Features.Loan.LoanV9;

public record IssueLoanInitiated(int UserId, long ApplicationId, ChannelTypeEnum Channel) : BpmEvent;

public record GeneratedContract(int UserId, ChannelTypeEnum Channel, bool IsHeader, string Language) : BpmEvent;

public record GeneratedSchedule(long CustomerId, ChannelTypeEnum Channel) : BpmEvent, IRequest<bool>;