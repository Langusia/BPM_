using Core.BPM.BCommand;
using MyCredo.Common;
using MyCredo.Retail.Loan.Domain.Models;

namespace MyCredo.Retail.Loan.Application.Features.RequestLoanProcess.CarPawnshop.Finishing;

public record FinishedRequestCarPawnshop(int UserId, ChannelTypeEnum Channel) : BpmEvent;