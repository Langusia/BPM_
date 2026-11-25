using BPM.Contracts;
using BPM.Core.Attributes;
using BPM.Core.Definition;
using BPM.Core.Definition.Interfaces;
using BPM.Core.Events;
using BPM.Core.Process;
using MediatR;

namespace BPM.Tests.Perf;

// =====================================================================
// Kitchen-sink perf fixture — the SDK-wide performance contract.
//
// RULE: every node type the SDK supports MUST appear in this graph.
// When a new node type is added to BPM.Core, extend this fixture in the
// same PR, or the perf contract silently stops covering it.
//
// Currently covered:
//   Node (linear)            OpenCase → … → CloseCase
//   GroupNode (parallel)     UploadIdDoc ∥ UploadIncomeDoc
//   ConditionalNode          Amount > 10_000 → deep branch / else fast
//   GroupNode nested in If   VerifyIncome ∥ VerifyCollateral
//   Or (alternative branch)  FastCheck | ManualCheck
//   AnyTimeNode              RefreshRates (also the stream-padding lever)
//   GuestProcessNode         JumpTo<ComplianceReview>
//   Nested configure lambdas, THREE levels deep:
//     Continue<NotifyCustomer>(n => n                        level 1
//       .Continue<ScheduleFollowUp>(s => s                   level 2
//         .Group(PrepareDocsPack ∥ ReserveSlot)              fork at level 2
//         .Continue<ConfirmPlan>(c => c                      level 3
//           .If(...).Else(...))))                            2-way branch at level 3
//     — exercises the _orScopeBuilder path in ProcessBuilder.Continue/Or,
//       which wires PrevSteps differently from top-level chaining, at depth
//   Group inside configure   PrepareDocsPack ∥ ReserveSlot (level-2 fork; PrevSteps
//     sourced from the enclosing configure scope, not the process root)
//   If nested at 3rd level   Amount > 30_000 → AssignManager / else brochure alternatives
//   UnlockOptional           RequestDiscount (OptionalNode inside the nested If branch)
//   Or inside configure scope SendBrochure | SkipBrochure (scoped-Or PrevSteps wiring)
//   OrAnyTime                PingCustomer (AnyTimeNode as an Or-alternative)
//   OrJumpTo                 QuickAudit (second guest process as an Or-alternative;
//     deliberately NOT ComplianceReview — its events are in the stream and would
//     make the never-taken alternative read as completed)
//
// Deliberately NOT covered (document, don't add silently):
//   Case / Case<T>           CaseInternal ignores its predicate and creates no
//     node — it invokes the configure lambda on the same builder, i.e. it
//     degenerates to a plain chain today. Unfinished feature; add here when
//     it actually builds a node. (Repo finding, 2026-07-09.)
//   OrOptional               exists on ProcessBuilder but is missing from
//     IProcessNodeModifiableBuilder, so process definitions (which see only
//     the interfaces) cannot call it. (Repo finding, 2026-07-09.)
// =====================================================================

// ---- events (main process) ----

public record CaseOpened(decimal Amount) : BpmEvent;
public record IdDocUploaded : BpmEvent;
public record IncomeDocUploaded : BpmEvent;
public record DeepCheckPassed : BpmEvent;
public record IncomeVerified : BpmEvent;
public record CollateralVerified : BpmEvent;
public record FastCheckPassed : BpmEvent;
public record ManualCheckPassed : BpmEvent;
public record OfferPrepared : BpmEvent;
public record RatesRefreshed : BpmEvent;
public record CaseClosed : BpmEvent;

// events for the nested-configure section:
public record CustomerNotified : BpmEvent;
public record FollowUpScheduled : BpmEvent;
public record DocsPackPrepared : BpmEvent;
public record SlotReserved : BpmEvent;
public record PlanConfirmed : BpmEvent;
public record ManagerAssigned : BpmEvent;
public record BrochureSent : BpmEvent;
public record BrochureSkipped : BpmEvent;
public record CustomerPinged : BpmEvent;
public record DiscountRequested : BpmEvent;

// ---- events (guest processes) ----

public record ReviewOpened : BpmEvent;
public record ReviewApproved : BpmEvent;
public record AuditRan : BpmEvent;

// ---- commands (main process) ----

[BpmProducer(typeof(CaseOpened))]
[BpmPolicy(ExecutionPolicy.Autonomous)]
[BpmDescription("Opens a kitchen-sink case.")]
public record OpenCase(decimal Amount) : IRequest<Guid>;

[BpmProducer(typeof(IdDocUploaded))]
[BpmPolicy(ExecutionPolicy.Autonomous)]
public record UploadIdDoc(Guid ProcessId) : IRequest;

[BpmProducer(typeof(IncomeDocUploaded))]
[BpmPolicy(ExecutionPolicy.Autonomous)]
public record UploadIncomeDoc(Guid ProcessId) : IRequest;

[BpmProducer(typeof(DeepCheckPassed))]
[BpmPolicy(ExecutionPolicy.Autonomous)]
public record DeepCheck(Guid ProcessId) : IRequest;

[BpmProducer(typeof(IncomeVerified))]
[BpmPolicy(ExecutionPolicy.Autonomous)]
public record VerifyIncome(Guid ProcessId) : IRequest;

[BpmProducer(typeof(CollateralVerified))]
[BpmPolicy(ExecutionPolicy.Autonomous)]
public record VerifyCollateral(Guid ProcessId) : IRequest;

[BpmProducer(typeof(FastCheckPassed))]
[BpmPolicy(ExecutionPolicy.Autonomous)]
public record FastCheck(Guid ProcessId) : IRequest;

[BpmProducer(typeof(ManualCheckPassed))]
[BpmPolicy(ExecutionPolicy.Autonomous)]
public record ManualCheck(Guid ProcessId) : IRequest;

[BpmProducer(typeof(OfferPrepared))]
[BpmPolicy(ExecutionPolicy.Autonomous)]
public record PrepareOffer(Guid ProcessId) : IRequest;

[BpmProducer(typeof(RatesRefreshed))]
[BpmPolicy(ExecutionPolicy.Autonomous)]
[BpmDescription("Anytime utility node; also used to pad streams to arbitrary length in perf runs.")]
public record RefreshRates(Guid ProcessId) : IRequest;

[BpmProducer(typeof(CaseClosed))]
[BpmPolicy(ExecutionPolicy.Autonomous)]
public record CloseCase(Guid ProcessId) : IRequest;

// commands for the nested-configure section:

[BpmProducer(typeof(CustomerNotified))]
[BpmPolicy(ExecutionPolicy.Autonomous)]
public record NotifyCustomer(Guid ProcessId) : IRequest;

[BpmProducer(typeof(FollowUpScheduled))]
[BpmPolicy(ExecutionPolicy.Autonomous)]
public record ScheduleFollowUp(Guid ProcessId) : IRequest;

[BpmProducer(typeof(DocsPackPrepared))]
[BpmPolicy(ExecutionPolicy.Autonomous)]
public record PrepareDocsPack(Guid ProcessId) : IRequest;

[BpmProducer(typeof(SlotReserved))]
[BpmPolicy(ExecutionPolicy.Autonomous)]
public record ReserveSlot(Guid ProcessId) : IRequest;

[BpmProducer(typeof(PlanConfirmed))]
[BpmPolicy(ExecutionPolicy.Autonomous)]
public record ConfirmPlan(Guid ProcessId) : IRequest;

[BpmProducer(typeof(ManagerAssigned))]
[BpmPolicy(ExecutionPolicy.Autonomous)]
public record AssignManager(Guid ProcessId) : IRequest;

[BpmProducer(typeof(BrochureSent))]
[BpmPolicy(ExecutionPolicy.Autonomous)]
public record SendBrochure(Guid ProcessId) : IRequest;

[BpmProducer(typeof(BrochureSkipped))]
[BpmPolicy(ExecutionPolicy.Autonomous)]
public record SkipBrochure(Guid ProcessId) : IRequest;

[BpmProducer(typeof(CustomerPinged))]
[BpmPolicy(ExecutionPolicy.Autonomous)]
public record PingCustomer(Guid ProcessId) : IRequest;

[BpmProducer(typeof(DiscountRequested))]
[BpmPolicy(ExecutionPolicy.Autonomous)]
public record RequestDiscount(Guid ProcessId) : IRequest;

// ---- commands (guest processes) ----

[BpmProducer(typeof(ReviewOpened))]
[BpmPolicy(ExecutionPolicy.Autonomous)]
public record OpenReview(Guid ProcessId) : IRequest;

[BpmProducer(typeof(ReviewApproved))]
[BpmPolicy(ExecutionPolicy.Autonomous)]
public record ApproveReview(Guid ProcessId) : IRequest;

[BpmProducer(typeof(AuditRan))]
[BpmPolicy(ExecutionPolicy.Autonomous)]
public record RunAudit(Guid ProcessId) : IRequest;

// ---- aggregates ----

[BpmDescription("Kitchen-sink perf fixture aggregate.")]
public class KitchenSink : Aggregate
{
    public decimal Amount { get; set; }
    public bool IdDoc { get; set; }
    public bool IncomeDoc { get; set; }
    public bool DeepChecked { get; set; }
    public bool IncomeOk { get; set; }
    public bool CollateralOk { get; set; }
    public bool FastChecked { get; set; }
    public bool ManualChecked { get; set; }
    public bool OfferReady { get; set; }
    public int RateRefreshes { get; set; }
    public bool Notified { get; set; }
    public bool FollowUpPlanned { get; set; }
    public bool DocsPacked { get; set; }
    public bool SlotHeld { get; set; }
    public bool PlanSet { get; set; }
    public bool ManagerOnCase { get; set; }
    public bool BrochureOut { get; set; }
    public bool BrochureDropped { get; set; }
    public int Pings { get; set; }
    public bool DiscountAsked { get; set; }
    public bool Closed { get; set; }

    public override bool? IsCompleted() => Closed;

    public void Apply(CaseOpened e) => Amount = e.Amount;
    public void Apply(IdDocUploaded e) => IdDoc = true;
    public void Apply(IncomeDocUploaded e) => IncomeDoc = true;
    public void Apply(DeepCheckPassed e) => DeepChecked = true;
    public void Apply(IncomeVerified e) => IncomeOk = true;
    public void Apply(CollateralVerified e) => CollateralOk = true;
    public void Apply(FastCheckPassed e) => FastChecked = true;
    public void Apply(ManualCheckPassed e) => ManualChecked = true;
    public void Apply(OfferPrepared e) => OfferReady = true;
    public void Apply(RatesRefreshed e) => RateRefreshes++;
    public void Apply(CustomerNotified e) => Notified = true;
    public void Apply(FollowUpScheduled e) => FollowUpPlanned = true;
    public void Apply(DocsPackPrepared e) => DocsPacked = true;
    public void Apply(SlotReserved e) => SlotHeld = true;
    public void Apply(PlanConfirmed e) => PlanSet = true;
    public void Apply(ManagerAssigned e) => ManagerOnCase = true;
    public void Apply(BrochureSent e) => BrochureOut = true;
    public void Apply(BrochureSkipped e) => BrochureDropped = true;
    public void Apply(CustomerPinged e) => Pings++;
    public void Apply(DiscountRequested e) => DiscountAsked = true;
    public void Apply(CaseClosed e) => Closed = true;
}

[BpmDescription("Guest review process for the kitchen-sink fixture.")]
public class ComplianceReview : Aggregate
{
    public bool Opened { get; set; }
    public bool Approved { get; set; }

    public override bool? IsCompleted() => Approved;

    public void Apply(ReviewOpened e) => Opened = true;
    public void Apply(ReviewApproved e) => Approved = true;
}

[BpmDescription("Second, never-driven guest process — OrJumpTo alternative target.")]
public class QuickAudit : Aggregate
{
    public bool Ran { get; set; }

    public override bool? IsCompleted() => Ran;

    public void Apply(AuditRan e) => Ran = true;
}

// ---- definitions ----

public class ComplianceReviewDefinition : BpmDefinition<ComplianceReview>
{
    public override ProcessConfig<ComplianceReview> DefineProcess(IProcessBuilder<ComplianceReview> configureProcess) =>
        configureProcess
            .StartWith<OpenReview>()
            .Continue<ApproveReview>()
            .End();
}

public class QuickAuditDefinition : BpmDefinition<QuickAudit>
{
    public override ProcessConfig<QuickAudit> DefineProcess(IProcessBuilder<QuickAudit> configureProcess) =>
        configureProcess
            .StartWith<RunAudit>()
            .End();
}

public class KitchenSinkDefinition : BpmDefinition<KitchenSink>
{
    public override ProcessConfig<KitchenSink> DefineProcess(IProcessBuilder<KitchenSink> configureProcess) =>
        configureProcess
            .StartWith<OpenCase>()
            .Group(g =>
            {
                g.AddStep<UploadIdDoc>();
                g.AddStep<UploadIncomeDoc>();
            })
            .If(p => p.Amount > 10_000, deep => deep
                .Continue<DeepCheck>()
                .Group(g =>
                {
                    g.AddStep<VerifyIncome>();
                    g.AddStep<VerifyCollateral>();
                }))
            .Else(fast => fast
                .Continue<FastCheck>()
                .Or<ManualCheck>())
            .Continue<PrepareOffer>()
            // Nested configure-lambda branching: Continue-in-Continue with an If
            // whose branches carry UnlockOptional and the scoped Or family.
            .Continue<NotifyCustomer>(n => n                      // ── level 1
                .Continue<ScheduleFollowUp>(s => s                // ── level 2
                    .Group(g =>                                   //    fork at level 2 (parallel),
                    {                                             //    before any branching
                        g.AddStep<PrepareDocsPack>();
                        g.AddStep<ReserveSlot>();
                    })
                    .Continue<ConfirmPlan>(c => c                 // ── level 3
                        .If(p => p.Amount > 30_000, vip => vip    //    branches into 2 here
                            .Continue<AssignManager>()
                            .UnlockOptional<RequestDiscount>())
                        .Else(std => std
                            .Continue<SendBrochure>()
                            .Or<SkipBrochure>()
                            .OrAnyTime<PingCustomer>()
                            .OrJumpTo<QuickAudit>()))))
            .ContinueAnyTime<RefreshRates>()
            .JumpTo<ComplianceReview>()
            .Continue<CloseCase>()
            .End();
}
