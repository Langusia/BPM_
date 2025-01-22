using Core.BPM.Application.Managers;
using Core.BPM.Attributes;
using Credo.Core.Shared.Library;
using Credo.Core.Shared.Mediator;

namespace MyCredo.Features.Loan.UploadImage;

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

    public UploadImageHandler()
    {
    }

    public async Task<Result<bool>> Handle(UploadImage request, CancellationToken cancellationToken)
    {

        //var ss = s.AppendEvent(x => x.Upload());
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