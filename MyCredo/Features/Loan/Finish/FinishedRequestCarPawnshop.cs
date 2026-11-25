using BPM.Core.Attributes;
using BPM.Core.Events;
using MyCredo.Common;

namespace MyCredo.Features.Loan.Finish;

public record FinishedRequestCarPawnshop(int UserId, ChannelTypeEnum Channel) : BpmEvent;