using Core.BPM.BCommand;
using MyCredo.Retail.Loan.Domain.Models.LoanApplication.Enums;

namespace MyCredo.Retail.Loan.Application.Features.RequestLoanProcess.CarPawnshop.UploadingImage;

public record UploadedImage(string Image, string Extension, string ImageName, long CustomerId, DigitalLoanProductTypeEnum ProductType) : BpmEvent;

