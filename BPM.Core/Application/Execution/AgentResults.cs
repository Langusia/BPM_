using System;
using System.Collections.Generic;
using BPM.Contracts;

namespace BPM.Core.Application.Execution;

/// <summary>
/// Structured, instructive outcome of an agent-facing operation. Errors say
/// what failed and what would be valid — never stack traces.
/// </summary>
public sealed record AgentResult<T>(T? Value, AgentError? Error)
{
    public bool Ok => Error is null;

    public static AgentResult<T> Success(T value) => new(value, null);
    public static AgentResult<T> Fail(AgentError error) => new(default, error);
    public static AgentResult<T> Fail(string code, string message, string? hint = null, object? details = null) =>
        new(default, new AgentError(code, message, hint, details));
}

public sealed record AgentError(string Code, string Message, string? Hint = null, object? Details = null);

/// <summary>Stable error codes returned to agents.</summary>
public static class AgentErrorCodes
{
    public const string ProcessNotFound = "process_not_found";
    public const string UnknownProcessType = "unknown_process_type";
    public const string UnknownCommand = "unknown_command";
    public const string AmbiguousCommand = "ambiguous_command";
    public const string CommandNotAvailable = "command_not_available";
    public const string NotAnInitialCommand = "not_an_initial_command";
    public const string InvalidArguments = "invalid_arguments";
    public const string PolicyHumanOnly = "policy_human_only";
    public const string ApprovalRequired = "approval_required";
    public const string IdentityUnresolved = "identity_unresolved";
    public const string ExecutionFailed = "execution_failed";
    public const string StartReturnedNoProcessId = "start_returned_no_process_id";
}

/// <summary>A registered process type as presented to agents.</summary>
public sealed record ProcessTypeSummary(
    string Name,
    string? Description,
    IReadOnlyList<CommandSummary> Commands);

/// <summary>
/// A command as presented to agents in listings and next-step results.
/// <paramref name="ExecutableByAgent"/> is true only for
/// <see cref="ExecutionPolicy.Autonomous"/> commands — approval-gated commands
/// can be proposed but not executed by an agent.
/// </summary>
public sealed record CommandSummary(
    string Name,
    string? Description,
    ExecutionPolicy Policy,
    bool IsInitial,
    bool ExecutableByAgent);

/// <summary>Current state of a process instance.</summary>
public sealed record ProcessStateResult(
    Guid ProcessId,
    string ProcessType,
    object? State,
    bool? IsCompleted,
    DateTimeOffset? StartedAt,
    IReadOnlyList<CommandSummary> NextSteps);

/// <summary>
/// Result of executing (or starting) a command: resulting state plus next steps
/// in the same response, so the agent never needs an immediate follow-up read.
/// </summary>
public sealed record ExecutionResult(
    Guid ProcessId,
    string ProcessType,
    object? HandlerResponse,
    object? State,
    bool? IsCompleted,
    IReadOnlyList<CommandSummary> NextSteps);

/// <summary>One event in a process instance's timeline.</summary>
public sealed record HistoryEntry(
    long Version,
    string EventType,
    DateTimeOffset Timestamp,
    object Data);
