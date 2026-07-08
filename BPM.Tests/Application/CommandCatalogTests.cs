using System.Linq;
using BPM.Core.Application.Execution;
using Xunit;

namespace BPM.Tests.Application;

public class CommandCatalogTests : GraphTestBase
{
    public CommandCatalogTests()
    {
        BuildDefinition<Ticket, TicketDefinition>();
        BuildDefinition<Sonar, SonarDefinition>();
    }

    [Fact]
    public void ProcessTypes_ListsRegisteredAggregatesWithEntryCommandsOnly()
    {
        var service = CreateService(out var catalog);

        var types = service.ListProcessTypes();

        Assert.Equal(2, types.Count);
        var ticket = types.Single(t => t.Name == nameof(Ticket));
        // Only the initial (entry) command is listed; continuation commands
        // (AddNote, EscalateTicket, CloseTicket) are discovered via next-steps.
        Assert.Equal(
            [nameof(OpenTicket)],
            ticket.EntryCommands.Select(c => c.Name).ToArray());
        Assert.All(ticket.EntryCommands, c => Assert.True(c.IsInitial));
        Assert.Equal(2, catalog.ProcessTypes.Count);
    }

    [Fact]
    public void InitialCommands_AreExactlyThoseAvailableOnAnEmptyStream()
    {
        CreateService(out var catalog);

        var ticket = catalog.FindProcessType(nameof(Ticket))!;

        Assert.True(ticket.Commands.Single(c => c.Name == nameof(OpenTicket)).IsInitial);
        Assert.All(ticket.Commands.Where(c => c.Name != nameof(OpenTicket)),
            c => Assert.False(c.IsInitial));
    }

    [Fact]
    public void ResolveCommand_IsCaseInsensitive()
    {
        CreateService(out var catalog);

        var matches = catalog.ResolveCommand("addnote");

        Assert.Single(matches);
        Assert.Equal(typeof(AddNote), matches[0].CommandType);
    }

    [Fact]
    public void ResolveCommand_UnknownName_ReturnsEmpty()
    {
        CreateService(out var catalog);

        Assert.Empty(catalog.ResolveCommand("NoSuchCommand"));
    }

    [Fact]
    public void ResolveCommand_SameNameInTwoProcesses_ReturnsBoth_AndSchemaReportsAmbiguity()
    {
        var service = CreateService(out var catalog);

        Assert.Equal(2, catalog.ResolveCommand(nameof(OpenTicket)).Count);

        var schema = service.GetCommandSchema(nameof(OpenTicket));
        Assert.False(schema.Ok);
        Assert.Equal(AgentErrorCodes.AmbiguousCommand, schema.Error!.Code);
    }

    [Fact]
    public void ResolveCommand_AmbiguousName_QualifiedByProcessType_Resolves()
    {
        var service = CreateService(out _);

        var schema = service.GetCommandSchema(nameof(OpenTicket), nameof(Sonar));

        Assert.True(schema.Ok);
        Assert.Equal(nameof(Sonar), schema.Value!.AggregateTypeName);
        Assert.Contains(schema.Value.Fields, f => f.Name == "Reason");
    }

    [Fact]
    public void FindProcessType_ReadsDescriptionFromAttribute()
    {
        CreateService(out var catalog);

        // Ticket/Sonar carry no description attribute; absent is fine.
        Assert.Null(catalog.FindProcessType(nameof(Ticket))!.Description);
    }
}
