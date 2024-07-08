using Core.BPM.Application.Managers;
using Core.BPM.MediatR.Attributes;
using Credo.Core.Shared.Library;
using Credo.Core.Shared.Mediator;
using Microsoft.Extensions.Options;
using MyCredo.Retail.Loan.Domain.Models.LoanApplication.Enums;

namespace MyCredo.Retail.Loan.Application.Features.RequestLoanProcess.CarPawnshop.UploadingImage;

[BpmProducer(typeof(UploadedImage))]
public record UploadImage : ICommand<bool>
{
    public string Image { get; set; }
    public string Extension { get; set; }
    public string ImageName { get; set; }
    public long CustomerId { get; set; }
    public DigitalLoanProductTypeEnum ProductType { get; set; }
    public Guid ProcessId { get; set; }
};

internal class UploadImageHandler : ICommandHandler<UploadImage, bool>
{
    private readonly BpmStore<RequestCarPawnshop, UploadImage> _bpm;

    public UploadImageHandler(
        BpmStore<RequestCarPawnshop, UploadImage> bpm)
    {
        _bpm = bpm;
    }

    public async Task<Result<bool>> Handle(UploadImage request, CancellationToken cancellationToken)
    {
        var s = await _bpm.AggregateProcessStateAsync(request.ProcessId, cancellationToken);


        return Result.Success(true);
    }

    private async Task<bool> UploadImage(string imagePath, string userFolderPath, string image, CancellationToken cancellationToken)
    {
        try
        {
            using var stream = new FileStream(imagePath, FileMode.Create, FileAccess.Write);
            using var memoryStream = new MemoryStream(Convert.FromBase64String(image));
            await memoryStream.CopyToAsync(stream, cancellationToken);
            return true;
        }
        catch (OperationCanceledException)
        {
            if (Directory.Exists(userFolderPath))
                Directory.Delete(userFolderPath, true);
            return false;
        }
    }
}