using Core.BPM.BCommand;

namespace MyCredo.Features.Loan.UploadImage;

public record UploadedImage(string Image, string Extension, string ImageName, long CustomerId, DigitalLoanProductTypeEnum ProductType) : BpmEvent;

