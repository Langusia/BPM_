namespace Loan.Contracts;

/// <summary>
/// The bank's identity object. Populated server-side from the authenticated
/// transport (JWT claims); never supplied by an agent.
/// </summary>
public sealed record UserContext(string UserId, string FullName, string? Email);
