# BPM.Core

A lightweight, code-first Business Process Management engine for .NET. Define stateful workflows as directed graphs using a fluent builder API, execute them step-by-step with event sourcing, and let the engine enforce ordering, branching, and validation at runtime.

Built on [Marten](https://martendb.io/) (PostgreSQL event store) and [MediatR](https://github.com/jbogard/MediatR).

## Table of Contents

- [Installation](#installation)
- [Quick Start](#quick-start)
- [Core Concepts](#core-concepts)
- [Defining a Process](#defining-a-process)
  - [Sequential Steps](#sequential-steps)
  - [Alternative Paths (Or)](#alternative-paths-or)
  - [Conditional Branches (If / Else)](#conditional-branches-if--else)
  - [Parallel Groups](#parallel-groups)
  - [Optional Steps](#optional-steps)
  - [AnyTime Steps](#anytime-steps)
  - [Guest Process (JumpTo)](#guest-process-jumpto)
  - [Process Configuration](#process-configuration)
- [Running a Process](#running-a-process)
- [Complex Branching Example](#complex-branching-example)
- [API Reference](#api-reference)

## Installation

Add a project reference to `BPM.Core`:

```xml
<ProjectReference Include="..\BPM.Core\BPM.Core.csproj" />
```

Required packages (already included in BPM.Core):

- `Marten` 7.7.0
- `MediatR` 14.0.0
- `Microsoft.Extensions.DependencyInjection`

## Quick Start

### 1. Define events

Every command produces one or more events. Events inherit from `BpmEvent`:

```csharp
public record OrderPlaced(string CustomerId, decimal Total) : BpmEvent;
public record OrderApproved() : BpmEvent;
public record OrderShipped(string TrackingNumber) : BpmEvent;
```

### 2. Define commands

Commands are MediatR requests decorated with `[BpmProducer]` to declare which events they emit:

```csharp
[BpmProducer(typeof(OrderPlaced))]
public record PlaceOrder(string CustomerId, decimal Total) : IRequest<Guid>;

[BpmProducer(typeof(OrderApproved))]
public record ApproveOrder(Guid ProcessId) : IRequest;

[BpmProducer(typeof(OrderShipped))]
public record ShipOrder(Guid ProcessId) : IRequest;
```

### 3. Define the aggregate

The aggregate holds the current state, rebuilt from events via `Apply()` methods:

```csharp
public class Order : Aggregate
{
    public string CustomerId { get; set; } = "";
    public decimal Total { get; set; }
    public bool IsApproved { get; set; }

    public void Apply(OrderPlaced e)   { CustomerId = e.CustomerId; Total = e.Total; }
    public void Apply(OrderApproved e) { IsApproved = true; }
    public void Apply(OrderShipped e)  { }
}
```

### 4. Define the process

Subclass `BpmDefinition<T>` and use the fluent builder to describe the workflow:

```csharp
public class OrderDefinition : BpmDefinition<Order>
{
    public override ProcessConfig<Order> DefineProcess(IProcessBuilder<Order> builder)
    {
        return builder
            .StartWith<PlaceOrder>()
            .Continue<ApproveOrder>()
            .Continue<ShipOrder>()
            .End();
    }
}
```

### 5. Register at startup

```csharp
builder.Services.AddBpm("bpm", connectionString, config =>
{
    config.AddAggregateDefinition<Order, OrderDefinition>();
});
```

### 6. Write command handlers

```csharp
// Starting a process
public class PlaceOrderHandler(IProcessStore store) : IRequestHandler<PlaceOrder, Guid>
{
    public async Task<Guid> Handle(PlaceOrder req, CancellationToken ct)
    {
        var process = store.StartProcess<Order>(
            new OrderPlaced(req.CustomerId, req.Total));
        await store.SaveChangesAsync(ct);
        return process.Id;
    }
}

// Continuing a process
public class ApproveOrderHandler(IProcessStore store) : IRequestHandler<ApproveOrder>
{
    public async Task Handle(ApproveOrder req, CancellationToken ct)
    {
        var process = await store.FetchProcessAsync(req.ProcessId, ct);
        process.AppendEvent(new OrderApproved());
        await store.SaveChangesAsync(ct);
    }
}
```

## Core Concepts

| Concept | Description |
|---------|-------------|
| **Aggregate** | Domain object whose state is rebuilt from an event stream. Subclass `Aggregate` and add `Apply(TEvent)` methods. |
| **BpmEvent** | Base record for all process events. Carries a `NodeId` used internally for graph traversal. |
| **Command** | A MediatR `IRequest` decorated with `[BpmProducer(typeof(TEvent))]`. Represents an action that advances the process. |
| **BpmDefinition** | Abstract class where you wire up the process graph using the fluent builder API. |
| **Node** | A vertex in the process graph. Each node maps to a command and knows its successors and predecessors. |
| **IProcess** | Runtime handle to a process instance. Append events, validate commands, query next steps. |
| **IProcessStore** | Creates, fetches, and persists process instances. |

## Defining a Process

All definitions start with `BpmDefinition<TAggregate>` and use the builder returned by `IProcessBuilder<T>`.

### Sequential Steps

The simplest flow: one step after another.

```csharp
builder
    .StartWith<InitiateRegistration>()
    .Continue<VerifyEmail>()
    .Continue<SetupProfile>()
    .Continue<CompleteRegistration>()
    .End();
```

```
InitiateRegistration -> VerifyEmail -> SetupProfile -> CompleteRegistration
```

### Alternative Paths (Or)

Allow the user to take one of several paths at a given step:

```csharp
builder
    .StartWith<SubmitApplication>()
    .Continue<ApproveViaManager>()
        .Or<ApproveViaDirector>()
        .Or<AutoApprove>()
    .Continue<FinalizeApplication>()
    .End();
```

```
                       +--> ApproveViaManager --+
                       |                        |
SubmitApplication -----+--> ApproveViaDirector -+--> FinalizeApplication
                       |                        |
                       +--> AutoApprove --------+
```

### Conditional Branches (If / Else)

Branch based on the current aggregate state. The predicate is evaluated at runtime:

```csharp
builder
    .StartWith<SubmitClaim>()
    .Continue<ReviewClaim>()
    .If(x => x.ClaimAmount > 10_000,
        branch => branch.Continue<ManagerApproval>())
    .Else(
        branch => branch.Continue<AutoApproval>())
    .Continue<IssuePayout>()
    .End();
```

```
                                  +--> ManagerApproval --+
SubmitClaim -> ReviewClaim -> IF -|                      +--> IssuePayout
                                  +--> AutoApproval ----+
```

### Parallel Groups

Execute multiple steps in any order. The group completes when all members are done:

```csharp
builder
    .StartWith<OpenAccount>()
    .Group(g =>
    {
        g.AddStep<VerifyIdentity>();
        g.AddStep<VerifyAddress>();
        g.AddStep<RunCreditCheck>();
    })
    .Continue<ActivateAccount>()
    .End();
```

```
                    +--> VerifyIdentity --+
                    |                     |
OpenAccount -> GROUP+--> VerifyAddress ---+--> ActivateAccount
                    |                     |
                    +--> RunCreditCheck --+
```

### Optional Steps

Unlock a step conditionally. It becomes available but is not required to proceed:

```csharp
builder
    .StartWith<PlaceOrder>()
    .Continue<ProcessPayment>()
    .If(x => x.IsPaid,
        branch => branch.UnlockOptional<ShipOrder>())
    .Continue<CompleteOrder>()
    .End();
```

### AnyTime Steps

Steps that can be executed at any point once they become available (not bound by strict ordering):

```csharp
builder
    .StartWith<StartOnboarding>()
    .ContinueAnyTime<UploadDocuments>()   // can be done at any point from here on
    .Continue<ScheduleInterview>()
    .End();
```

### Guest Process (JumpTo)

Delegate to another aggregate's process before continuing:

```csharp
builder
    .StartWith<CreateLoan>()
    .Continue<SubmitForApproval>()
    .JumpTo<CreditCheckAggregate>(sealedSteps: true)
    .Continue<DisburseFunds>()
    .End();
```

When `sealedSteps: true`, the guest process steps are hidden from the parent's available steps after the guest process completes.

### Process Configuration

Pass options via `.End(config => ...)`:

```csharp
builder
    .StartWith<Begin>()
    .Continue<Finish>()
    .End(config =>
    {
        config.ExpirationSeconds = 3600; // process expires after 1 hour
    });
```

## Running a Process

### Start a new process

```csharp
var process = store.StartProcess<Order>(new OrderPlaced("cust-1", 99.99m));
await store.SaveChangesAsync(ct);
Guid processId = process.Id;
```

### Fetch and continue

```csharp
var process = await store.FetchProcessAsync(processId, ct);

// Validate before executing
var validation = process.Validate<ApproveOrder>();
if (!validation.IsSuccess) return BadRequest(validation.Code);

// Append event and save
process.AppendEvent(new OrderApproved());
await store.SaveChangesAsync(ct);
```

### Read aggregate state

```csharp
var process = await store.FetchProcessAsync(processId, ct);
var order = process.AggregateAs<Order>();
// order.IsApproved == true
```

### Query available next steps

```csharp
var result = process.GetNextSteps();
var availableCommands = result.Data; // List<INode> - each node has .CommandType
```

### Handle failures

```csharp
process.AppendFail<Order>("Payment declined", new { Reason = "Insufficient funds" });
await store.SaveChangesAsync(ct);
```

## Complex Branching Example

Here is a complete example of a **loan application** process that uses every branching feature:

```
                                          +--> ManualKycReview -----+
                                          |                         |
                      +---> IF(highRisk) -+                         |
                      |                   +--> AutoKycApproval ----+|
                      |                                             |
SubmitApplication --> CreditCheck --+--> IF(score > 700) ----------++--> GROUP +--> SignContract
                      |             |                               |          |
                      |             +--> DenyCreditApplication      |          +--> UploadDocuments
                      |                                             |          |
                      +---> RequestCoSigner(AnyTime)                |          +--> SetupAutoPayment
                                                                    |
                                                          FinalApproval
```

### Events

```csharp
public record ApplicationSubmitted(string ApplicantId, decimal Amount) : BpmEvent;
public record CreditChecked(int Score, bool IsHighRisk) : BpmEvent;
public record CoSignerRequested(string CoSignerName) : BpmEvent;
public record ManualKycCompleted(bool Passed) : BpmEvent;
public record AutoKycCompleted() : BpmEvent;
public record CreditDenied(string Reason) : BpmEvent;
public record FinalApprovalGranted() : BpmEvent;
public record ContractSigned(DateTime SignedAt) : BpmEvent;
public record DocumentsUploaded(string[] FileNames) : BpmEvent;
public record AutoPaymentConfigured(string AccountNumber) : BpmEvent;
```

### Commands

```csharp
[BpmProducer(typeof(ApplicationSubmitted))]
public record SubmitApplication(string ApplicantId, decimal Amount) : IRequest<Guid>;

[BpmProducer(typeof(CreditChecked))]
public record RunCreditCheck(Guid ProcessId) : IRequest;

[BpmProducer(typeof(CoSignerRequested))]
public record RequestCoSigner(Guid ProcessId, string CoSignerName) : IRequest;

[BpmProducer(typeof(ManualKycCompleted))]
public record ManualKycReview(Guid ProcessId) : IRequest;

[BpmProducer(typeof(AutoKycCompleted))]
public record AutoKycApproval(Guid ProcessId) : IRequest;

[BpmProducer(typeof(CreditDenied))]
public record DenyCreditApplication(Guid ProcessId, string Reason) : IRequest;

[BpmProducer(typeof(FinalApprovalGranted))]
public record GrantFinalApproval(Guid ProcessId) : IRequest;

[BpmProducer(typeof(ContractSigned))]
public record SignContract(Guid ProcessId) : IRequest;

[BpmProducer(typeof(DocumentsUploaded))]
public record UploadDocuments(Guid ProcessId, string[] Files) : IRequest;

[BpmProducer(typeof(AutoPaymentConfigured))]
public record SetupAutoPayment(Guid ProcessId, string AccountNumber) : IRequest;
```

### Aggregate

```csharp
public class LoanApplication : Aggregate
{
    public string ApplicantId { get; set; } = "";
    public decimal Amount { get; set; }
    public int CreditScore { get; set; }
    public bool IsHighRisk { get; set; }
    public bool IsCreditApproved { get; set; }
    public bool IsKycPassed { get; set; }

    public void Apply(ApplicationSubmitted e) { ApplicantId = e.ApplicantId; Amount = e.Amount; }
    public void Apply(CreditChecked e)        { CreditScore = e.Score; IsHighRisk = e.IsHighRisk; }
    public void Apply(CoSignerRequested e)    { }
    public void Apply(ManualKycCompleted e)   { IsKycPassed = e.Passed; }
    public void Apply(AutoKycCompleted e)     { IsKycPassed = true; }
    public void Apply(CreditDenied e)         { IsCreditApproved = false; }
    public void Apply(FinalApprovalGranted e) { IsCreditApproved = true; }
    public void Apply(ContractSigned e)       { }
    public void Apply(DocumentsUploaded e)    { }
    public void Apply(AutoPaymentConfigured e){ }
}
```

### Process Definition

```csharp
public class LoanApplicationDefinition : BpmDefinition<LoanApplication>
{
    public override ProcessConfig<LoanApplication> DefineProcess(
        IProcessBuilder<LoanApplication> builder)
    {
        return builder
            .StartWith<SubmitApplication>()

            // Credit check with a co-signer request available at any time
            .Continue<RunCreditCheck>()
                .OrAnyTime<RequestCoSigner>()

            // Branch on credit score
            .If(x => x.CreditScore > 700,

                // Good credit: KYC depends on risk level
                approved => approved
                    .If(x => x.IsHighRisk,
                        highRisk => highRisk.Continue<ManualKycReview>())
                    .Else(
                        lowRisk => lowRisk.Continue<AutoKycApproval>())
                    .Continue<GrantFinalApproval>())

            // Bad credit: deny
            .Else(denied => denied
                .Continue<DenyCreditApplication>())

            // Parallel closing tasks
            .Group(g =>
            {
                g.AddStep<SignContract>();
                g.AddStep<UploadDocuments>();
                g.AddStep<SetupAutoPayment>();
            })
            .End(config =>
            {
                config.ExpirationSeconds = 7 * 24 * 3600; // 7 days to complete
            });
    }
}
```

### Handler Examples

```csharp
public class SubmitApplicationHandler(IProcessStore store)
    : IRequestHandler<SubmitApplication, Guid>
{
    public async Task<Guid> Handle(SubmitApplication req, CancellationToken ct)
    {
        var process = store.StartProcess<LoanApplication>(
            new ApplicationSubmitted(req.ApplicantId, req.Amount));
        await store.SaveChangesAsync(ct);
        return process.Id;
    }
}

public class RunCreditCheckHandler(IProcessStore store)
    : IRequestHandler<RunCreditCheck>
{
    public async Task Handle(RunCreditCheck req, CancellationToken ct)
    {
        var process = await store.FetchProcessAsync(req.ProcessId, ct);

        // Call external credit service...
        int score = 750;
        bool highRisk = false;

        process.AppendEvent(new CreditChecked(score, highRisk));
        await store.SaveChangesAsync(ct);
    }
}
```

### Registration

```csharp
builder.Services.AddBpm("bpm", connectionString, config =>
{
    config.AddAggregateDefinition<LoanApplication, LoanApplicationDefinition>();
});
```

## API Reference

### Builder Methods

| Method | Description |
|--------|-------------|
| `StartWith<TCmd>()` | Begin the process with a required command |
| `StartWithAnyTime<TCmd>()` | Begin with a flexible-order command |
| `Continue<TCmd>()` | Add the next sequential step |
| `ContinueAnyTime<TCmd>()` | Add a step executable at any point from here on |
| `Or<TCmd>()` | Add an alternative to the previous step |
| `OrAnyTime<TCmd>()` | Alternative as an any-time step |
| `OrJumpTo<TAggregate>()` | Alternative that delegates to another process |
| `If(predicate, branch)` | Conditional branch evaluated against aggregate state |
| `Else(branch)` | Else branch for a preceding `If` |
| `Group(configure)` | Parallel execution group (all must complete) |
| `UnlockOptional<TCmd>()` | Optional step unlocked by a condition |
| `JumpTo<TAggregate>(sealed)` | Delegate to a guest process |
| `End(configure?)` | Finalize the definition |

### IProcess Methods

| Method | Description |
|--------|-------------|
| `AggregateAs<T>()` | Rebuild aggregate from events |
| `AggregateOrNullAs<T>()` | Safe version returning `null` |
| `TryAggregateAs<T>(out T?)` | Try-pattern aggregate rebuild |
| `AppendEvent(BpmEvent)` | Validate and queue an event |
| `ForceAppendEvents(params object[])` | Queue events without validation |
| `AppendFail<T>(desc, data)` | Record a process failure |
| `Validate<T>()` | Check if a command can execute now |
| `GetNextSteps()` | Get currently available commands |
| `SaveChangesAsync()` | Persist to event store |

### IProcessStore Methods

| Method | Description |
|--------|-------------|
| `StartProcess<T>(BpmEvent)` | Create a new process with an initial event |
| `StartProcess<T>()` | Create a new process without an initial event |
| `FetchProcessAsync(Guid, CancellationToken)` | Load a process by ID |
| `SaveChangesAsync(CancellationToken)` | Persist all pending changes |

### Result Types

```csharp
BpmResult          // { IsSuccess, Code }
BpmResult<T>       // { IsSuccess, Code, Data }

enum Code { Success, NoSuccess, ProcessFailed, InvalidEvent, Expired }
```

## Infrastructure

BPM.Core uses PostgreSQL via Marten for event storage. The included `docker-compose.yml` sets up the required database:

```bash
docker compose up -d
```

Connection string format:

```
Host=localhost;Port=5432;Database=bpm_db;Username=bpm_user;Password=bpm_password
```

## License

See LICENSE file for details.
