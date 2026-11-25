using System.Security.Claims;
using BPM.Core.Application.Execution;
using Loan.Contracts;
using MediatR;

namespace Loan.Api;

/// <summary>
/// The human channel: plain HTTP endpoints over the same MediatR pipeline the
/// MCP tools dispatch through. No agent policy gate here — this is where a
/// human signs the contract the agent had to stop at, and where HumanOnly
/// disbursement actually happens.
/// </summary>
public static class LoanEndpoints
{
    public record InitiateLoanRequest(decimal Amount, int TermMonths, LoanPurpose Purpose);
    public record SubmitCollateralRequest(string VehicleVin, int VehicleYear, decimal EstimatedValue);
    public record SignContractRequest(string? Note);

    public static UserContext MapUser(ClaimsPrincipal user) => new(
        user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub") ?? "unknown",
        user.FindFirstValue(ClaimTypes.Name) ?? user.FindFirstValue("name") ?? "Unknown User",
        user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue("email"));

    public static IEndpointRouteBuilder MapLoanEndpoints(this IEndpointRouteBuilder app)
    {
        var loans = app.MapGroup("/loans").WithTags("Loans").RequireAuthorization();

        loans.MapPost("/initiate",
                async (InitiateLoanRequest request, IMediator mediator, ClaimsPrincipal user, CancellationToken ct) =>
                {
                    var processId = await mediator.Send(new InitiateLoanApplication(
                        request.Amount, request.TermMonths, request.Purpose, MapUser(user)), ct);
                    return Results.Ok(new { processId });
                })
            .WithSummary("Start a loan application");

        loans.MapPost("/{processId:guid}/collateral",
                async (Guid processId, SubmitCollateralRequest request, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new SubmitCollateral(
                        processId, request.VehicleVin, request.VehicleYear, request.EstimatedValue), ct);
                    return Results.NoContent();
                })
            .WithSummary("Register the vehicle collateral");

        loans.MapPost("/{processId:guid}/price",
                async (Guid processId, IMediator mediator, CancellationToken ct) =>
                {
                    await mediator.Send(new PriceLoan(processId), ct);
                    return Results.NoContent();
                })
            .WithSummary("Run pricing (rate is computed server-side)");

        loans.MapPost("/{processId:guid}/sign",
                async (Guid processId, SignContractRequest request, IMediator mediator, ClaimsPrincipal user,
                    CancellationToken ct) =>
                {
                    await mediator.Send(new SignLoanContract(processId, request.Note, MapUser(user)), ct);
                    return Results.NoContent();
                })
            .WithSummary("Sign the contract — the step agents stop at with approval_required");

        loans.MapPost("/{processId:guid}/disburse",
                async (Guid processId, IMediator mediator, ClaimsPrincipal user, CancellationToken ct) =>
                {
                    await mediator.Send(new DisburseLoan(processId, MapUser(user)), ct);
                    return Results.NoContent();
                })
            .WithSummary("Pay out the loan — HumanOnly, never reachable via MCP");

        loans.MapGet("/{processId:guid}",
                async (Guid processId, IAgentProcessService processes, CancellationToken ct) =>
                {
                    var result = await processes.GetProcessAsync(processId, ct);
                    return result.Ok ? Results.Ok(result.Value) : Results.NotFound(result.Error);
                })
            .WithSummary("Current state and next steps (same projection the MCP tools serve)");

        return app;
    }
}
