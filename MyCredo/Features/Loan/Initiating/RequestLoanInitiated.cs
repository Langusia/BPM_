using Core.BPM.BCommand;
using MyCredo.Retail.Loan.Domain.Models.LoanApplication.Enums;

namespace MyCredo.Retail.Loan.Application.Features.RequestLoanProcess.CarPawnshop.Initiating;

public record RequestLoanInitiated(DigitalLoanProductTypeEnum ProductType, string? PromoCode, decimal Percent, int? TryCount,decimal Amount, decimal EffectiveInterestRate, int Period) : BpmEvent;
