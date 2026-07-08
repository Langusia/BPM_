using System.ComponentModel;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using BPM.Core.Application.Execution;
using Microsoft.AspNetCore.Http;
using ModelContextProtocol.Server;

namespace BPM.Mcp;

/// <summary>
/// The generic BPM dispatch tools. One instance is activated per tool call;
/// the caller's identity is read from the request's authenticated HttpContext,
/// never from tool arguments.
/// </summary>
[McpServerToolType]
public sealed class BpmMcpTools(IAgentProcessService service, IHttpContextAccessor httpContextAccessor)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        WriteIndented = true
    };

    private ClaimsPrincipal? Caller => httpContextAccessor.HttpContext?.User;

    private const string LanguageParam =
        "ISO 639-1 code of the language the user is currently writing in (e.g. \"en\" English, " +
        "\"ka\" Georgian). Set it to match the user's language so command and field descriptions " +
        "come back localized; switch it whenever the user switches language. It does not affect " +
        "execution, only the descriptive text returned. Defaults to English.";

    [McpServerTool(Name = "bpm_list_process_types", ReadOnly = true, Idempotent = true)]
    [Description("Lists every business process type this service hosts, with each process's commands, " +
                 "their execution policy, and which commands can start a new instance.")]
    public string ListProcessTypes(
        [Description(LanguageParam)] string language = "en") =>
        Ok(service.ListProcessTypes(language));

    [McpServerTool(Name = "bpm_get_process", ReadOnly = true, Idempotent = true)]
    [Description("Gets the current state of a process instance: its aggregate state, completion status, " +
                 "and the commands currently available as next steps.")]
    public async Task<string> GetProcess(
        [Description("The process instance id (GUID) returned by bpm_start_process.")] Guid processId,
        [Description(LanguageParam)] string language = "en",
        CancellationToken cancellationToken = default) =>
        Render(await service.GetProcessAsync(processId, cancellationToken, language));

    [McpServerTool(Name = "bpm_get_next_steps", ReadOnly = true, Idempotent = true)]
    [Description("Lists the commands that can be executed next on a process instance, including each " +
                 "command's execution policy (autonomous / requires approval / human-only).")]
    public async Task<string> GetNextSteps(
        [Description("The process instance id (GUID).")] Guid processId,
        [Description(LanguageParam)] string language = "en",
        CancellationToken cancellationToken = default) =>
        Render(await service.GetNextStepsAsync(processId, Caller, cancellationToken, language));

    [McpServerTool(Name = "bpm_get_history", ReadOnly = true, Idempotent = true)]
    [Description("Returns the event timeline of a process instance: every recorded event with its " +
                 "version, timestamp, and data.")]
    public async Task<string> GetHistory(
        [Description("The process instance id (GUID).")] Guid processId,
        CancellationToken cancellationToken = default) =>
        Render(await service.GetHistoryAsync(processId, cancellationToken));

    [McpServerTool(Name = "bpm_get_command_schema", ReadOnly = true, Idempotent = true)]
    [Description("Returns the JSON Schema for a command's arguments, plus its execution policy and " +
                 "success criteria. Call this before bpm_start_process or bpm_execute_command to learn " +
                 "which fields to send. Server-populated fields (identity, computed values) are not in " +
                 "the schema and must not be sent.")]
    public string GetCommandSchema(
        [Description("The command name, e.g. \"InitiateLoanApplication\".")] string commandName,
        [Description("Optional process type name to disambiguate commands that exist in several processes.")]
        string? processType = null,
        [Description(LanguageParam)] string language = "en")
    {
        var result = service.GetCommandSchema(commandName, processType, language);
        if (!result.Ok)
            return Error(result.Error!);

        var model = result.Value!;
        return Ok(new JsonObject
        {
            ["command"] = model.CommandName,
            ["processType"] = model.AggregateTypeName,
            ["isInitial"] = model.IsInitial,
            ["executionPolicy"] = JsonNamingPolicy.CamelCase.ConvertName(model.Policy.ToString()),
            ["successCriteria"] = model.SuccessCriteria,
            ["argumentsSchema"] = JsonSchemaSerializer.ToJsonSchema(model)
        });
    }

    [McpServerTool(Name = "bpm_start_process")]
    [Description("Starts a new process instance by executing one of its initial commands. Returns the new " +
                 "process id, the resulting process state, and the next available steps — no follow-up " +
                 "read is needed. Arguments must match bpm_get_command_schema for the command.")]
    public async Task<string> StartProcess(
        [Description("The process type name from bpm_list_process_types, e.g. \"LoanApplication\".")]
        string processType,
        [Description("The initial command name, e.g. \"InitiateLoanApplication\".")] string commandName,
        [Description("The command arguments as a JSON object string matching the command schema.")]
        string? argsJson = null,
        [Description(LanguageParam)] string language = "en",
        CancellationToken cancellationToken = default) =>
        Render(await service.StartProcessAsync(processType, commandName, argsJson, Caller, cancellationToken, language));

    [McpServerTool(Name = "bpm_execute_command")]
    [Description("Executes a command on an existing process instance. Returns the resulting process state " +
                 "and next available steps in the same response — no follow-up read is needed. The engine " +
                 "enforces step ordering and execution policy server-side; commands requiring approval are " +
                 "not executed and return a structured approval_required error.")]
    public async Task<string> ExecuteCommand(
        [Description("The process instance id (GUID).")] Guid processId,
        [Description("The command name from bpm_get_next_steps.")] string commandName,
        [Description("The command arguments as a JSON object string matching the command schema. " +
                     "Do not include the process id or any identity fields; those come from the transport.")]
        string? argsJson = null,
        [Description(LanguageParam)] string language = "en",
        CancellationToken cancellationToken = default) =>
        Render(await service.ExecuteCommandAsync(processId, commandName, argsJson, Caller, cancellationToken, language));

    // ---- rendering ----

    private static string Render<T>(AgentResult<T> result) =>
        result.Ok ? Ok(result.Value!) : Error(result.Error!);

    private static string Ok(object value) =>
        JsonSerializer.Serialize(new { ok = true, result = value }, JsonOptions);

    private static string Error(AgentError error) =>
        JsonSerializer.Serialize(new { ok = false, error }, JsonOptions);
}
