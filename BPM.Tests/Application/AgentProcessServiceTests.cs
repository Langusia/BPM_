using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BPM.Contracts;
using BPM.Core.Application.Execution;
using Xunit;

namespace BPM.Tests.Application;

public class AgentProcessServiceTests : GraphTestBase
{
    private static readonly Guid ProcessId = Guid.NewGuid();

    public AgentProcessServiceTests()
    {
        BuildDefinition<Ticket, TicketDefinition>();
        Options.Identity.RegisterGlobal(
            typeof(TestIdentity),
            (user, _) => Task.FromResult<object?>(new TestIdentity(
                user.FindFirst(ClaimTypes.NameIdentifier)!.Value,
                user.FindFirst(ClaimTypes.Name)!.Value)));
    }

    private void SeedTicket(params object[] events) =>
        Store.Seed(ProcessId, nameof(Ticket), events);

    private object Opened() =>
        new TicketOpened("Printer on fire", "someone") { NodeId = NodeLevelOf<Ticket, OpenTicket>() };

    private object Noted() =>
        new NoteAdded("checked") { NodeId = NodeLevelOf<Ticket, AddNote>() };

    private object Escalated() =>
        new TicketEscalated { NodeId = NodeLevelOf<Ticket, EscalateTicket>() };

    // ---- reads ----

    [Fact]
    public async Task GetProcess_UnknownId_ReturnsStructuredNotFound()
    {
        var service = CreateService(out _);

        var result = await service.GetProcessAsync(Guid.NewGuid(), default);

        Assert.False(result.Ok);
        Assert.Equal(AgentErrorCodes.ProcessNotFound, result.Error!.Code);
        Assert.NotNull(result.Error.Hint);
    }

    [Fact]
    public async Task GetProcess_ProjectsAggregateStateAndNextSteps()
    {
        SeedTicket(Opened());
        var service = CreateService(out _);

        var result = await service.GetProcessAsync(ProcessId, default);

        Assert.True(result.Ok);
        var state = Assert.IsType<Ticket>(result.Value!.State);
        Assert.Equal("Printer on fire", state.Subject);
        Assert.Equal(ProcessId, state.Id);
        Assert.Equal([nameof(AddNote)], result.Value.NextSteps.Select(s => s.Name).ToArray());
        Assert.False(result.Value.IsCompleted);
    }

    [Fact]
    public async Task GetNextSteps_ReportsPolicyAndAgentExecutability()
    {
        SeedTicket(Opened(), Noted());
        var service = CreateService(out _);

        var result = await service.GetNextStepsAsync(ProcessId, AuthenticatedUser(), default);

        Assert.True(result.Ok);
        var step = Assert.Single(result.Value!);
        Assert.Equal(nameof(EscalateTicket), step.Name);
        Assert.Equal(ExecutionPolicy.HumanOnly, step.Policy);
        Assert.False(step.ExecutableByAgent);
    }

    [Fact]
    public async Task GetHistory_ReturnsOrderedTimeline()
    {
        SeedTicket(Opened(), Noted());
        var service = CreateService(out _);

        var result = await service.GetHistoryAsync(ProcessId, default);

        Assert.True(result.Ok);
        Assert.Equal(
            [nameof(TicketOpened), nameof(NoteAdded)],
            result.Value!.Select(h => h.EventType).ToArray());
        Assert.Equal([1L, 2L], result.Value.Select(h => h.Version).ToArray());
    }

    // ---- policy gate (physical, server-side) ----

    [Fact]
    public async Task HumanOnlyCommand_IsNeverDispatched()
    {
        SeedTicket(Opened(), Noted());
        var service = CreateService(out _);

        var result = await service.ExecuteCommandAsync(
            ProcessId, nameof(EscalateTicket), null, AuthenticatedUser(), default);

        Assert.False(result.Ok);
        Assert.Equal(AgentErrorCodes.PolicyHumanOnly, result.Error!.Code);
        Assert.Empty(Dispatcher.Dispatched);
    }

    [Fact]
    public async Task UnannotatedCommand_DefaultsToRequiresApproval_AndIsNotDispatched()
    {
        SeedTicket(Opened());
        var service = CreateService(out _);

        var result = await service.ExecuteCommandAsync(
            ProcessId, nameof(AddNote), """{"text":"agent note"}""", AuthenticatedUser(), default);

        Assert.False(result.Ok);
        Assert.Equal(AgentErrorCodes.ApprovalRequired, result.Error!.Code);
        Assert.Contains("NOT executed", result.Error.Message);
        Assert.Empty(Dispatcher.Dispatched);
    }

    // ---- engine-level availability ----

    [Fact]
    public async Task CommandThatIsNotANextStep_IsRejectedWithAvailableAlternatives()
    {
        SeedTicket(Opened());
        var service = CreateService(out _);

        // CloseTicket is autonomous but not reachable yet.
        var result = await service.ExecuteCommandAsync(
            ProcessId, nameof(CloseTicket), null, AuthenticatedUser(), default);

        Assert.False(result.Ok);
        Assert.Equal(AgentErrorCodes.CommandNotAvailable, result.Error!.Code);
        Assert.Empty(Dispatcher.Dispatched);
    }

    [Fact]
    public async Task UnknownCommand_ListsKnownCommands()
    {
        SeedTicket(Opened());
        var service = CreateService(out _);

        var result = await service.ExecuteCommandAsync(
            ProcessId, "FrobnicateTicket", null, AuthenticatedUser(), default);

        Assert.False(result.Ok);
        Assert.Equal(AgentErrorCodes.UnknownCommand, result.Error!.Code);
    }

    // ---- argument binding ----

    [Fact]
    public async Task MissingRequiredArguments_ReturnInstructiveError_BeforeDispatch()
    {
        var service = CreateService(out _);

        var result = await service.StartProcessAsync(
            nameof(Ticket), nameof(OpenTicket), "{}", AuthenticatedUser(), default);

        Assert.False(result.Ok);
        Assert.Equal(AgentErrorCodes.InvalidArguments, result.Error!.Code);
        Assert.Contains("Subject", result.Error.Message);
        Assert.Empty(Dispatcher.Dispatched);
    }

    [Fact]
    public async Task MalformedArgsJson_ReturnsInvalidArguments()
    {
        var service = CreateService(out _);

        var result = await service.StartProcessAsync(
            nameof(Ticket), nameof(OpenTicket), "not-json", AuthenticatedUser(), default);

        Assert.False(result.Ok);
        Assert.Equal(AgentErrorCodes.InvalidArguments, result.Error!.Code);
    }

    // ---- identity: transport wins, agent-supplied values are discarded ----

    [Fact]
    public async Task IdentityComesFromPrincipal_AgentSuppliedIdentityIsDiscardedAndLogged()
    {
        SeedTicket(Opened(), Noted(), Escalated());
        Dispatcher.OnDispatch = _ => Task.FromResult<object?>(null);
        var service = CreateService(out _);

        var spoofed = """{"identity":{"userId":"EVIL","name":"Mallory"}}""";
        var result = await service.ExecuteCommandAsync(
            ProcessId, nameof(CloseTicket), spoofed, AuthenticatedUser("u-42", "Alice"), default);

        Assert.True(result.Ok);
        var dispatched = Assert.IsType<CloseTicket>(Assert.Single(Dispatcher.Dispatched));
        Assert.Equal("u-42", dispatched.Identity!.UserId);
        Assert.Equal("Alice", dispatched.Identity.Name);
        Assert.Equal(ProcessId, dispatched.ProcessId);
        Assert.Contains(Logger.Messages, m => m.Contains("Discarded") && m.Contains("Identity"));
    }

    [Fact]
    public async Task UnauthenticatedCaller_CannotExecuteIdentityCarryingCommand()
    {
        SeedTicket(Opened(), Noted(), Escalated());
        var service = CreateService(out _);

        var result = await service.ExecuteCommandAsync(
            ProcessId, nameof(CloseTicket), null, new ClaimsPrincipal(new ClaimsIdentity()), default);

        Assert.False(result.Ok);
        Assert.Equal(AgentErrorCodes.IdentityUnresolved, result.Error!.Code);
        Assert.Empty(Dispatcher.Dispatched);
    }

    // ---- execute + next steps in one response ----

    [Fact]
    public async Task Execute_ReturnsResultingStateAndNextSteps_NoFollowUpReadNeeded()
    {
        SeedTicket(Opened(), Noted(), Escalated());
        Dispatcher.OnDispatch = _ =>
        {
            // Simulate the handler appending the produced event.
            Store.Seed(ProcessId, nameof(Ticket),
                new TicketClosed("Alice") { NodeId = NodeLevelOf<Ticket, CloseTicket>() });
            return Task.FromResult<object?>(null);
        };
        var service = CreateService(out _);

        var result = await service.ExecuteCommandAsync(
            ProcessId, nameof(CloseTicket), null, AuthenticatedUser("u-42", "Alice"), default);

        Assert.True(result.Ok);
        var state = Assert.IsType<Ticket>(result.Value!.State);
        Assert.True(state.Closed);
        Assert.True(result.Value.IsCompleted);
        Assert.Empty(result.Value.NextSteps);
    }

    // ---- start process ----

    [Fact]
    public async Task StartProcess_DispatchesInitialCommand_AndReturnsNewProcessWithNextSteps()
    {
        var newId = Guid.NewGuid();
        Dispatcher.OnDispatch = cmd =>
        {
            var open = (OpenTicket)cmd;
            Store.Seed(newId, nameof(Ticket),
                new TicketOpened(open.Subject, open.Identity!.UserId)
                {
                    NodeId = NodeLevelOf<Ticket, OpenTicket>()
                });
            return Task.FromResult<object?>(newId);
        };
        var service = CreateService(out _);

        var result = await service.StartProcessAsync(
            nameof(Ticket), nameof(OpenTicket), """{"subject":"Printer on fire"}""",
            AuthenticatedUser("u-42", "Alice"), default);

        Assert.True(result.Ok);
        Assert.Equal(newId, result.Value!.ProcessId);
        var state = Assert.IsType<Ticket>(result.Value.State);
        Assert.Equal("u-42", state.OpenedBy);
        Assert.Equal([nameof(AddNote)], result.Value.NextSteps.Select(s => s.Name).ToArray());
    }

    [Fact]
    public async Task StartProcess_WithContinuationCommand_IsRejected()
    {
        var service = CreateService(out _);

        var result = await service.StartProcessAsync(
            nameof(Ticket), nameof(AddNote), """{"text":"hi"}""", AuthenticatedUser(), default);

        Assert.False(result.Ok);
        Assert.Equal(AgentErrorCodes.NotAnInitialCommand, result.Error!.Code);
        Assert.Empty(Dispatcher.Dispatched);
    }

    [Fact]
    public async Task StartProcess_UnknownProcessType_ListsRegisteredTypes()
    {
        var service = CreateService(out _);

        var result = await service.StartProcessAsync(
            "Mortgage", nameof(OpenTicket), null, AuthenticatedUser(), default);

        Assert.False(result.Ok);
        Assert.Equal(AgentErrorCodes.UnknownProcessType, result.Error!.Code);
    }

    [Fact]
    public async Task StartProcess_HandlerNotReturningGuid_YieldsStructuredError()
    {
        Dispatcher.OnDispatch = _ => Task.FromResult<object?>("not-a-guid");
        var service = CreateService(out _);

        var result = await service.StartProcessAsync(
            nameof(Ticket), nameof(OpenTicket), """{"subject":"x"}""", AuthenticatedUser(), default);

        Assert.False(result.Ok);
        Assert.Equal(AgentErrorCodes.StartReturnedNoProcessId, result.Error!.Code);
    }

    [Fact]
    public async Task HandlerException_BecomesStructuredExecutionFailure_NotAStackTrace()
    {
        SeedTicket(Opened(), Noted(), Escalated());
        Dispatcher.OnDispatch = _ => throw new InvalidOperationException("collateral value too low");
        var service = CreateService(out _);

        var result = await service.ExecuteCommandAsync(
            ProcessId, nameof(CloseTicket), null, AuthenticatedUser(), default);

        Assert.False(result.Ok);
        Assert.Equal(AgentErrorCodes.ExecutionFailed, result.Error!.Code);
        Assert.Contains("collateral value too low", result.Error.Message);
        Assert.DoesNotContain("at ", result.Error.Message);
    }
}
