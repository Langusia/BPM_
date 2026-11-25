using System;
using System.Collections.Generic;
using BPM.Contracts;
using BPM.Core.Attributes;
using BPM.Core.Definition;
using BPM.Core.Definition.Interfaces;
using BPM.Core.Events;
using BPM.Core.Process;
using MediatR;

namespace BPM.Tests.Application;

// ---- identity ----

public record TestIdentity(string UserId, string Name);

// ---- ticket process: Open -> AddNote -> Escalate -> Close ----

public record TicketOpened(string Subject, string OpenedBy) : BpmEvent;
public record NoteAdded(string Text) : BpmEvent;
public record TicketEscalated : BpmEvent;
public record TicketClosed(string ClosedBy) : BpmEvent;

[BpmProducer(typeof(TicketOpened))]
[BpmPolicy(ExecutionPolicy.Autonomous)]
[BpmDescription("Opens a new support ticket.")]
public record OpenTicket(string Subject, TestIdentity? Identity = null)
    : IRequest<Guid>, IAuthenticatedRequest<TestIdentity>;

// No policy annotation: must fall back to the RequiresApproval default.
[BpmProducer(typeof(NoteAdded))]
public record AddNote(Guid ProcessId, string Text) : IRequest;

[BpmProducer(typeof(TicketEscalated))]
[BpmPolicy(ExecutionPolicy.HumanOnly)]
public record EscalateTicket(Guid ProcessId) : IRequest;

[BpmProducer(typeof(TicketClosed))]
[BpmPolicy(ExecutionPolicy.Autonomous)]
public record CloseTicket(Guid ProcessId, TestIdentity? Identity = null)
    : IRequest, IAuthenticatedRequest<TestIdentity>;

public class Ticket : Aggregate
{
    public string Subject { get; set; } = string.Empty;
    public string OpenedBy { get; set; } = string.Empty;
    public List<string> Notes { get; set; } = new();
    public bool Escalated { get; set; }
    public bool Closed { get; set; }
    public string? ClosedBy { get; set; }

    public override bool? IsCompleted() => Closed;

    public void Apply(TicketOpened e)
    {
        Subject = e.Subject;
        OpenedBy = e.OpenedBy;
    }

    public void Apply(NoteAdded e) => Notes.Add(e.Text);
    public void Apply(TicketEscalated e) => Escalated = true;

    public void Apply(TicketClosed e)
    {
        Closed = true;
        ClosedBy = e.ClosedBy;
    }
}

public class TicketDefinition : BpmDefinition<Ticket>
{
    public override ProcessConfig<Ticket> DefineProcess(IProcessBuilder<Ticket> configureProcess) =>
        configureProcess
            .StartWith<OpenTicket>()
            .Continue<AddNote>()
            .Continue<EscalateTicket>()
            .Continue<CloseTicket>()
            .End();
}

// ---- second process with a same-named command for ambiguity tests ----

public record PingReceived : BpmEvent;

public class Sonar : Aggregate
{
    public bool Pinged { get; set; }
    public void Apply(PingReceived e) => Pinged = true;
}

public class SonarDefinition : BpmDefinition<Sonar>
{
    public override ProcessConfig<Sonar> DefineProcess(IProcessBuilder<Sonar> configureProcess) =>
        configureProcess
            .StartWith<AmbiguousDomain.OpenTicket>()
            .End();
}
