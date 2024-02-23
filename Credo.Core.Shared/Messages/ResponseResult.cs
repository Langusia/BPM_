using Microsoft.AspNetCore.Mvc;

namespace Credo.Core.Shared.Messages;

public static class Extensions
{
    public static ActionResult<ApiServiceResponse<T>>AsResponseResult<T>(this ApiServiceResponse<T> response)
    {
        switch (response.State)
        {
            case ApiStatus.Ok:
                return new OkObjectResult(response);
            case ApiStatus.NotFound:
                return new NotFoundObjectResult(response);
            default:
                return new BadRequestObjectResult(response);
        }
    }

    public static ActionResult<ApiServiceResponse> AsResponseResult(this ApiServiceResponse response)
    {
        switch (response.State)
        {
            case ApiStatus.Ok:
                return new OkObjectResult(response);
            case ApiStatus.NotFound:
                return new NotFoundObjectResult(response);
            default:
                return new BadRequestObjectResult(response);
        }
    }
}
