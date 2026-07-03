using MediatR;

namespace Loan.Api;

/// <summary>
/// Ordinary MediatR pipeline behavior. Fires for every dispatch — HTTP or MCP —
/// because agent execution goes through the same pipeline as any other caller.
/// </summary>
public class CommandAuditBehavior<TRequest, TResponse>(ILogger<CommandAuditBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public static int InvocationCount;

    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref InvocationCount);
        logger.LogInformation("Dispatching {Command}", typeof(TRequest).Name);
        return await next();
    }
}
