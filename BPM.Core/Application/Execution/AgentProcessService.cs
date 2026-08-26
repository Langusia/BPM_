using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using BPM.Contracts;
using BPM.Core.Application.Catalog;
using BPM.Core.Application.Metadata;
using BPM.Core.Application.Persistence;
using BPM.Core.Application.Projection;
using BPM.Core.Configuration;
using BPM.Core.Persistence;
using BPM.Core.Process;
using Microsoft.Extensions.Logging;

namespace BPM.Core.Application.Execution;

public sealed class AgentProcessService(
    ICommandCatalog catalog,
    IProcessInstanceStore store,
    ICommandDispatcher dispatcher,
    CommandMetadataResolver metadataResolver,
    CommandSchemaProjector projector,
    BpmAgentOptions options,
    ProcessRegistry registry,
    IServiceProvider services,
    ILogger<AgentProcessService> logger) : IAgentProcessService
{
    private readonly CommandArgumentBinder _binder = new();

    public IReadOnlyList<ProcessTypeSummary> ListProcessTypes() =>
        catalog.ProcessTypes
            .Select(p => new ProcessTypeSummary(
                p.Name,
                p.Description,
                p.Commands.Select(Summarize).ToList()))
            .ToList();

    public async Task<AgentResult<ProcessStateResult>> GetProcessAsync(Guid processId, CancellationToken ct)
    {
        var snapshot = await store.LoadAsync(processId, ct);
        if (snapshot is null)
            return AgentResult<ProcessStateResult>.Fail(ProcessNotFound(processId));

        return AgentResult<ProcessStateResult>.Success(BuildProcessState(snapshot));
    }

    public async Task<AgentResult<IReadOnlyList<CommandSummary>>> GetNextStepsAsync(
        Guid processId, ClaimsPrincipal? caller, CancellationToken ct)
    {
        var snapshot = await store.LoadAsync(processId, ct);
        if (snapshot is null)
            return AgentResult<IReadOnlyList<CommandSummary>>.Fail(ProcessNotFound(processId));

        return AgentResult<IReadOnlyList<CommandSummary>>.Success(NextSteps(snapshot));
    }

    public async Task<AgentResult<IReadOnlyList<HistoryEntry>>> GetHistoryAsync(Guid processId, CancellationToken ct)
    {
        var snapshot = await store.LoadAsync(processId, ct);
        if (snapshot is null)
            return AgentResult<IReadOnlyList<HistoryEntry>>.Fail(ProcessNotFound(processId));

        var history = snapshot.Events
            .Select(e => new HistoryEntry(e.Version, e.EventTypeName, e.Timestamp, e.Data))
            .ToList();
        return AgentResult<IReadOnlyList<HistoryEntry>>.Success(history);
    }

    public AgentResult<CommandSchemaModel> GetCommandSchema(string commandName, string? processType = null)
    {
        var matches = catalog.ResolveCommand(commandName, processType);
        if (matches.Count == 0)
            return AgentResult<CommandSchemaModel>.Fail(UnknownCommand(commandName, processType));

        var distinctTypes = matches.Select(m => m.CommandType).Distinct().ToList();
        if (distinctTypes.Count > 1)
            return AgentResult<CommandSchemaModel>.Fail(new AgentError(
                AgentErrorCodes.AmbiguousCommand,
                $"Command name '{commandName}' exists in multiple process types: " +
                $"{string.Join(", ", matches.Select(m => m.AggregateTypeName).Distinct())}.",
                "Pass the processType argument to disambiguate."));

        return AgentResult<CommandSchemaModel>.Success(projector.Project(matches[0]));
    }

    public async Task<AgentResult<ExecutionResult>> StartProcessAsync(
        string processType, string commandName, string? argsJson, ClaimsPrincipal? caller, CancellationToken ct)
    {
        var descriptor = catalog.FindProcessType(processType);
        if (descriptor is null)
            return AgentResult<ExecutionResult>.Fail(new AgentError(
                AgentErrorCodes.UnknownProcessType,
                $"Unknown process type '{processType}'.",
                "Call bpm_list_process_types to see what is registered.",
                new { registeredProcessTypes = catalog.ProcessTypes.Select(p => p.Name).ToList() }));

        var command = descriptor.Commands.FirstOrDefault(c =>
            string.Equals(c.Name, commandName, StringComparison.OrdinalIgnoreCase));
        if (command is null)
            return AgentResult<ExecutionResult>.Fail(UnknownCommand(commandName, processType));

        if (!command.IsInitial)
            return AgentResult<ExecutionResult>.Fail(new AgentError(
                AgentErrorCodes.NotAnInitialCommand,
                $"'{command.Name}' cannot start a new '{processType}' process.",
                "Use bpm_execute_command with an existing processId for continuation commands.",
                new
                {
                    initialCommands = descriptor.Commands.Where(c => c.IsInitial).Select(c => c.Name).ToList()
                }));

        var prepared = await PrepareForDispatchAsync(command, argsJson, processId: null, caller);
        if (prepared.Error is not null)
            return AgentResult<ExecutionResult>.Fail(prepared.Error);

        object? response;
        try
        {
            response = await dispatcher.DispatchAsync(prepared.Command!, ct);
        }
        catch (Exception ex)
        {
            return AgentResult<ExecutionResult>.Fail(ExecutionFailed(command.Name, ex));
        }

        if (!TryExtractProcessId(response, out var newProcessId))
            return AgentResult<ExecutionResult>.Fail(new AgentError(
                AgentErrorCodes.StartReturnedNoProcessId,
                $"'{command.Name}' executed, but its handler did not return the new process id.",
                "Initial command handlers must return the Guid of the started process."));

        var snapshot = await store.LoadAsync(newProcessId, ct);
        return AgentResult<ExecutionResult>.Success(BuildExecutionResult(newProcessId, descriptor.Name, response, snapshot));
    }

    public async Task<AgentResult<ExecutionResult>> ExecuteCommandAsync(
        Guid processId, string commandName, string? argsJson, ClaimsPrincipal? caller, CancellationToken ct)
    {
        var snapshot = await store.LoadAsync(processId, ct);
        if (snapshot is null)
            return AgentResult<ExecutionResult>.Fail(ProcessNotFound(processId));

        var matches = catalog.ResolveCommand(commandName, snapshot.AggregateTypeName);
        if (matches.Count == 0)
            return AgentResult<ExecutionResult>.Fail(UnknownCommand(commandName, snapshot.AggregateTypeName));
        var command = matches[0];

        // Engine-level availability: the command must be an available next step
        // for the instance's current event stream.
        var availableTypes = AvailableNodeCommandTypes(snapshot);
        if (!availableTypes.Contains(command.CommandType))
        {
            var available = NextSteps(snapshot);
            return AgentResult<ExecutionResult>.Fail(new AgentError(
                AgentErrorCodes.CommandNotAvailable,
                $"'{command.Name}' is not an available step for process {processId} right now.",
                "Execute one of the currently available commands instead.",
                new { availableCommands = available.Select(s => s.Name).ToList() }));
        }

        var prepared = await PrepareForDispatchAsync(command, argsJson, processId, caller);
        if (prepared.Error is not null)
            return AgentResult<ExecutionResult>.Fail(prepared.Error);

        object? response;
        try
        {
            response = await dispatcher.DispatchAsync(prepared.Command!, ct);
        }
        catch (Exception ex)
        {
            return AgentResult<ExecutionResult>.Fail(ExecutionFailed(command.Name, ex));
        }

        var updated = await store.LoadAsync(processId, ct);
        return AgentResult<ExecutionResult>.Success(
            BuildExecutionResult(processId, snapshot.AggregateTypeName, response, updated));
    }

    // ---- dispatch preparation: policy gate → binding → identity population ----

    private sealed record Prepared(object? Command, AgentError? Error);

    private async Task<Prepared> PrepareForDispatchAsync(
        CatalogCommandDescriptor command, string? argsJson, Guid? processId, ClaimsPrincipal? caller)
    {
        var metadata = metadataResolver.Resolve(command.CommandType);

        // Server-side policy gate — physical, not a description string.
        switch (metadata.Policy)
        {
            case ExecutionPolicy.HumanOnly:
                return new Prepared(null, new AgentError(
                    AgentErrorCodes.PolicyHumanOnly,
                    $"'{command.Name}' is human-only and can never be executed through an agent.",
                    "A human operator must perform this step through the primary application."));
            case ExecutionPolicy.RequiresApproval:
                return new Prepared(null, new AgentError(
                    AgentErrorCodes.ApprovalRequired,
                    $"'{command.Name}' requires human approval and was NOT executed.",
                    "Report to the user that this step is pending approval; a human must execute it " +
                    "through an approved channel. Do not retry.",
                    new { command = command.Name, policy = nameof(ExecutionPolicy.RequiresApproval) }));
        }

        var bound = _binder.Bind(metadata, argsJson, processId);
        if (bound.DiscardedFields.Count > 0)
            logger.LogWarning(
                "Discarded agent-supplied value(s) for non-input field(s) [{Fields}] of command {Command}. " +
                "Identity and derived fields are populated server-side only.",
                string.Join(", ", bound.DiscardedFields), command.Name);
        if (bound.Error is not null)
            return new Prepared(null, bound.Error);

        var identityError = await PopulateIdentityAsync(bound.Command!, command.CommandType, caller);
        return new Prepared(bound.Command, identityError);
    }

    private async Task<AgentError?> PopulateIdentityAsync(object command, Type commandType, ClaimsPrincipal? caller)
    {
        var registration = options.Identity.Resolve(commandType);
        if (registration is null)
            return null;

        // With a declared identity type, a command that carries no such field
        // simply has no identity to populate. Object-typed registrations (per-
        // command resolvers with a runtime-only identity type) locate the
        // target after resolving.
        PropertyInfo? target = null;
        if (registration.IdentityType != typeof(object))
        {
            target = commandType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p => p.PropertyType.IsAssignableFrom(registration.IdentityType));
            if (target is null)
                return null; // command does not carry an identity field
        }

        if (caller?.Identity?.IsAuthenticated != true)
            return new AgentError(
                AgentErrorCodes.IdentityUnresolved,
                $"'{commandType.Name}' requires an authenticated caller identity.",
                "The MCP endpoint must be called with valid credentials; identity never comes from arguments.");

        object? identity;
        try
        {
            identity = await registration.Resolver(caller, services);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Identity resolver for {Command} threw.", commandType.Name);
            identity = null;
        }

        if (identity is null)
            return new AgentError(
                AgentErrorCodes.IdentityUnresolved,
                $"The caller's identity could not be mapped for '{commandType.Name}'.",
                "The authenticated principal is missing required claims.");

        target ??= commandType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p => p.PropertyType.IsInstanceOfType(identity));
        if (target is null)
            return null;

        target.SetValue(command, identity);
        return null;
    }

    // ---- projections over the event stream ----

    private ProcessStateResult BuildProcessState(ProcessInstanceSnapshot snapshot)
    {
        var (state, isCompleted) = ProjectAggregateState(snapshot);
        return new ProcessStateResult(
            snapshot.ProcessId,
            snapshot.AggregateTypeName,
            state,
            isCompleted,
            snapshot.StartedAt,
            NextSteps(snapshot));
    }

    private ExecutionResult BuildExecutionResult(
        Guid processId, string processType, object? handlerResponse, ProcessInstanceSnapshot? snapshot)
    {
        if (snapshot is null)
            return new ExecutionResult(processId, processType, handlerResponse, null, null, []);

        var (state, isCompleted) = ProjectAggregateState(snapshot);
        return new ExecutionResult(processId, processType, handlerResponse, state, isCompleted, NextSteps(snapshot));
    }

    private (object? State, bool? IsCompleted) ProjectAggregateState(ProcessInstanceSnapshot snapshot)
    {
        var descriptor = catalog.FindProcessType(snapshot.AggregateTypeName);
        if (descriptor is null)
            return (null, null);

        var aggregate = (Aggregate)FastActivator.CreateAggregate(descriptor.AggregateType)!;
        foreach (var envelope in snapshot.Events)
        {
            var apply = registry.GetApplyMethodOrNull(descriptor.AggregateType, envelope.Data.GetType());
            apply?.Invoke(aggregate, envelope.Data);
        }

        aggregate.Id = snapshot.ProcessId;
        return (aggregate, aggregate.IsCompleted());
    }

    private IReadOnlyList<CommandSummary> NextSteps(ProcessInstanceSnapshot snapshot) =>
        AvailableNodeCommandTypes(snapshot)
            .Select(t =>
            {
                var descriptor = catalog.ResolveCommand(t.Name, snapshot.AggregateTypeName).FirstOrDefault();
                return descriptor is null ? null : Summarize(descriptor);
            })
            .Where(s => s is not null)
            .Select(s => s!)
            .ToList();

    private IReadOnlyList<Type> AvailableNodeCommandTypes(ProcessInstanceSnapshot snapshot)
    {
        var process = BProcessGraphConfiguration.GetAllProcesses()
            ?.FirstOrDefault(p => p.ProcessType.Name == snapshot.AggregateTypeName);
        if (process is null)
            return [];

        var events = snapshot.Events.Select(e => e.Data).ToList();
        var (_, availableNodes) = process.RootNode
            .GetCheckBranchCompletionAndGetAvailableNodesFromCache(events);
        return availableNodes.Select(n => n.CommandType).Distinct().ToList();
    }

    private CommandSummary Summarize(CatalogCommandDescriptor command)
    {
        var metadata = metadataResolver.Resolve(command.CommandType);
        return new CommandSummary(
            command.Name,
            metadata.Description,
            metadata.Policy,
            command.IsInitial,
            metadata.Policy == ExecutionPolicy.Autonomous);
    }

    // ---- errors ----

    private AgentError ProcessNotFound(Guid processId) => new(
        AgentErrorCodes.ProcessNotFound,
        $"No process instance found with id {processId}.",
        "Check the id, or start a new process with bpm_start_process.");

    private AgentError UnknownCommand(string commandName, string? processType) => new(
        AgentErrorCodes.UnknownCommand,
        processType is null
            ? $"Unknown command '{commandName}'."
            : $"Unknown command '{commandName}' for process type '{processType}'.",
        "Call bpm_list_process_types to see every registered command.",
        new
        {
            knownCommands = (processType is null
                    ? catalog.ProcessTypes.SelectMany(p => p.Commands)
                    : catalog.FindProcessType(processType)?.Commands ?? [])
                .Select(c => c.Name).Distinct().ToList()
        });

    private static AgentError ExecutionFailed(string commandName, Exception ex) => new(
        AgentErrorCodes.ExecutionFailed,
        $"'{commandName}' failed during execution: {ex.GetBaseException().Message}",
        "The arguments were accepted but the handler rejected the operation. " +
        "Check the process state with bpm_get_process before retrying.");

    private static bool TryExtractProcessId(object? handlerResponse, out Guid processId)
    {
        switch (handlerResponse)
        {
            case Guid guid when guid != Guid.Empty:
                processId = guid;
                return true;
            case string s when Guid.TryParse(s, out var parsed) && parsed != Guid.Empty:
                processId = parsed;
                return true;
            default:
                processId = Guid.Empty;
                return false;
        }
    }
}
