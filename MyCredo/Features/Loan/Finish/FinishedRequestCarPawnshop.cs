using Core.BPM.BCommand;
using MyCredo.Common;

namespace MyCredo.Features.Loan.Finish;

public record FinishedRequestCarPawnshop(int UserId, ChannelTypeEnum Channel) : BpmEvent;