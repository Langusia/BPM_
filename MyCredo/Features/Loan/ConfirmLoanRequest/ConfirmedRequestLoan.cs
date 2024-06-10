using Core.BPM.BCommand;
using MyCredo.Common;
using MyCredo.Retail.Loan.Domain.Models;

namespace MyCredo.Retail.Loan.Application.Features.RequestLoanProcess.CarPawnshop.ConfirmRequestLoan;

public record ConfirmedCarPawnshop(string TraceId, int UserId, ChannelTypeEnum Channel, int? TryCount) : BpmEvent;