using BPM.Core.Attributes;
using BPM.Core.Events;

namespace MyCredo.Features.Loan.UploadImage;

public record UploadedImage(string Image, string Extension, string ImageName, long CustomerId, DigitalLoanProductTypeEnum ProductType) : BpmEvent;

