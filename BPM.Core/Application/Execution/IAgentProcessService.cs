using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using BPM.Core.Application.Projection;

namespace BPM.Core.Application.Execution;

/// <summary>
/// Transport-agnostic application service that agent adapters (BPM.Mcp today,
/// UI 2.0 later) call. The caller principal comes from the adapter's
/// authenticated transport; identity is never taken from arguments.
/// </summary>
public interface IAgentProcessService
{
    // language: ISO 639-1 code of the language the user is writing in, chosen by the
    // agent per call. Localized descriptions resolve to it, falling back to English.
    IReadOnlyList<ProcessTypeSummary> ListProcessTypes(string? language = null);

    Task<AgentResult<ProcessStateResult>> GetProcessAsync(Guid processId, CancellationToken ct, string? language = null);

    Task<AgentResult<IReadOnlyList<CommandSummary>>> GetNextStepsAsync(
        Guid processId, ClaimsPrincipal? caller, CancellationToken ct, string? language = null);

    Task<AgentResult<IReadOnlyList<HistoryEntry>>> GetHistoryAsync(Guid processId, CancellationToken ct);

    AgentResult<CommandSchemaModel> GetCommandSchema(string commandName, string? processType = null, string? language = null);

    /// <summary>Starts a new process instance by dispatching an initial command.</summary>
    Task<AgentResult<ExecutionResult>> StartProcessAsync(
        string processType, string commandName, string? argsJson, ClaimsPrincipal? caller, CancellationToken ct, string? language = null);

    /// <summary>Executes a command against an existing process instance.</summary>
    Task<AgentResult<ExecutionResult>> ExecuteCommandAsync(
        Guid processId, string commandName, string? argsJson, ClaimsPrincipal? caller, CancellationToken ct, string? language = null);
}
