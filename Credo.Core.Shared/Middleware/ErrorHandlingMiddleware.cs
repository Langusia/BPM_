using Credo.Core.Shared.Exceptions;
using Credo.Core.Shared.Messages;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace Credo.Core.Shared.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";
        switch (ex)
        {
            case ValidationException validationException:
                context.Response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
                var res = new ValidationFailedApiServiceResponse(validationException.Failures.FirstOrDefault().Key);

                foreach (var item in validationException.Failures)
                {
                    res.AddError($"{item.Key} : {item.Value[0]}");
                }
                return context.Response.WriteAsync(JsonSerializer.Serialize(res));
            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return context.Response.WriteAsync(JsonSerializer.Serialize(new InternalServiceFailedApiServiceResponse(ex)));
        }
    }
}
