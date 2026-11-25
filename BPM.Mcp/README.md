# BPM.Mcp

Turn any service hosting the [BPM_ engine](https://github.com/Langusia/BPM_) into its **own MCP server**. Install the package, call `.UseMcp()`, map one endpoint — and AI clients (Claude Desktop, MCP Inspector, a custom chat frontend) can discover, inspect, and drive your business processes over streamable HTTP.

This is the decentralized model: a bank's `Loan.Api` exposes only its own processes. There is no central governor.

```
BPM_.Contracts   ← attributes, ExecutionPolicy, IAgentSpec<T>, markers (near-zero deps)
BPM_.Core        ← engine + transport-agnostic application layer (catalog, schema projection, execute + next steps)
BPM_.Mcp         ← this package: thin MCP adapter over the application layer
```

## Philosophy

- **The agent decides WHAT to call; the transport decides WHO is calling.** Identity comes from the authenticated `HttpContext`, never from tool arguments. Agent-supplied identity or derived fields are discarded unconditionally and logged.
- **Approval policy is enforced server-side by the engine**, never by prompt instructions. A `RequiresApproval` command is physically not dispatched; a `HumanOnly` command is never executable through MCP at all.
- **The agent-facing schema is a projection of the command, never the raw record.**

## Install

```
dotnet add package BPM_.Mcp
```

Your shared contracts assembly (command records, events, agent specs) only needs:

```
dotnet add package BPM_.Contracts
```

## Wire-up

```csharp
builder.Services.AddBpm("bpm", connectionString,
        x => x.AddAggregateDefinition<LoanApplication, LoanApplicationDefinition>())
    .UseMcp(mcp => mcp
        // Global identity mapping: claims → your identity object.
        .WithIdentity<UserContext>(user => new UserContext(
            user.FindFirstValue(ClaimTypes.NameIdentifier)!,
            user.FindFirstValue(ClaimTypes.Name)!,
            user.FindFirstValue(ClaimTypes.Email)))
        // Optional per-command override (async, with services for enrichment lookups).
        .WithIdentityFor<SignLoanContract, UserContext>(async (user, sp) =>
        {
            var directory = sp.GetRequiredService<IEmployeeDirectory>();
            return await directory.ResolveAsync(user);
        })
        .WithAgentSpecsFromAssembly(typeof(UserContext).Assembly));

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();
app.MapMcp("/mcp").RequireAuthorization();   // host auth guards the endpoint
```

`UseMcp()` wires the official `ModelContextProtocol` server SDK with a **stateless streamable-HTTP transport**, so every tool call runs against its own authenticated `HttpContext`. Token validation (JWT, Duende, whatever) is the host's concern — BPM.Mcp only reads `HttpContext.User`.

Identity resolution order at execute time: **exact command type → base interface (e.g. `IAuthenticatedRequest<UserContext>`) → global default**.

## The tools

| Tool | What it does |
|---|---|
| `bpm_list_process_types` | Registered process types, their commands, policies, and which commands can start an instance |
| `bpm_start_process(processType, commandName, argsJson)` | Dispatches an initial command; returns new process id + resulting state + next steps |
| `bpm_get_process(processId)` | Current aggregate state, completion, next steps |
| `bpm_get_next_steps(processId)` | Commands currently available, with per-command policy |
| `bpm_get_command_schema(commandName, processType?)` | Agent-facing JSON Schema + policy + success criteria |
| `bpm_execute_command(processId, commandName, argsJson)` | Dispatch through the normal MediatR pipeline; returns resulting state + next steps in the same response |
| `bpm_get_history(processId)` | Event timeline |

`bpm_execute_command` / `bpm_start_process` return the resulting state and next steps directly, so agents never need an immediate follow-up read. Errors are structured and instructive (`code`, `message`, `hint`, `details` — e.g. the list of currently available commands), never stack traces.

## Field classification — three buckets

Every command property lands in exactly one bucket:

| Bucket | In schema? | Populated by | Marked with |
|---|---|---|---|
| **Agent input** | ✅ | the agent | default |
| **Server-populated** | ❌ | transport identity / route | property of a configured identity type, `ProcessId`, `[BpmServerPopulated]`, or `spec.Field(...).ServerPopulated()` |
| **Derived** | ❌ | server-side logic (pricing, etc.) | `[BpmDerived]` or `spec.Field(...).Derived()` |

Agent-supplied values for non-input fields are **silently discarded and logged**. C# enums become JSON Schema `enum` constraints; `[BpmPattern]`, `[BpmRange]`, `[BpmRequired]` and nullability become schema validation keywords.

## Execution policy — three tiers

```csharp
[BpmPolicy(ExecutionPolicy.Autonomous)]        // agent may execute directly
[BpmPolicy(ExecutionPolicy.RequiresApproval)]  // returns structured approval_required; NOT dispatched
[BpmPolicy(ExecutionPolicy.HumanOnly)]         // never executable via MCP
```

**Unannotated commands default to `RequiresApproval`** — safe by default. The gate lives in the execution path of the engine, before dispatch.

## Agent specs — richer metadata than attributes

Colocate a companion spec with the command; it is discovered by `WithAgentSpecsFromAssembly(...)`. Precedence: **fluent spec > attributes > reflection defaults**.

```csharp
public class InitiateLoanApplicationAgentSpec : IAgentSpec<InitiateLoanApplication>
{
    public void Configure(AgentSpecBuilder<InitiateLoanApplication> spec)
    {
        spec.Describe("Initiates a car-pawnshop loan for an authenticated customer.")
            .Policy(ExecutionPolicy.Autonomous)
            .SuccessCriteria("A new loan application exists and SubmitCollateral is the next step.");

        spec.Field(x => x.Amount)
            .Describe("Requested loan amount in GEL")
            .Source("Customer's stated request; confirm before executing")
            .Range(500, 50_000);

        spec.Field(x => x.EffectiveInterestRate).Derived();
        spec.Field(x => x.UserContext).ServerPopulated();
    }
}
```

Descriptions and source hints surface in the projected JSON Schema; the policy feeds the server-side gate. Commands without a spec still work with safe defaults.

## Dispatch goes through YOUR pipeline

MCP execution calls `IMediator.Send` — the same MediatR pipeline as any HTTP controller. Existing pipeline behaviors (auth, validation, auditing) fire on the MCP path. There is no second dispatch mechanism.

## Try it: MCP Inspector

Run the sample host (`samples/Loan.Api`, needs the repo's `docker-compose up` Postgres):

```bash
dotnet run --project samples/Loan.Api
TOKEN=$(curl -s http://localhost:5000/dev-token)

npx @modelcontextprotocol/inspector
```

In the Inspector UI choose **Streamable HTTP**, URL `http://localhost:5000/mcp`, and add a header `Authorization: Bearer <TOKEN>`. You can now list tools, fetch `bpm_get_command_schema("InitiateLoanApplication")`, start a process, and watch `SignLoanContract` come back as `approval_required`.

## Try it: Claude Desktop

Claude Desktop (v1) speaks stdio to local servers, so bridge it with `mcp-remote` and the dev token:

```jsonc
// claude_desktop_config.json
{
  "mcpServers": {
    "loan-api": {
      "command": "npx",
      "args": [
        "mcp-remote",
        "http://localhost:5000/mcp",
        "--header",
        "Authorization: Bearer <paste output of /dev-token>"
      ]
    }
  }
}
```

Restart Claude Desktop and ask: *"List the loan processes you can drive and start a 5000 GEL application."* The agent will stop, by construction, at the contract-signing approval gate.

> The `/dev-token` endpoint is development-only convenience. In production the MCP endpoint sits behind your real identity provider.

## Testing your own host

The application layer is transport-agnostic and fully unit-testable: implement the narrow `IProcessInstanceStore` port in memory (see `BPM.Mcp.Tests/InMemoryPersistence.cs` for a complete example) and drive the whole stack — MCP transport, JWT, identity population, policy gate, MediatR pipeline — with `WebApplicationFactory` and the official MCP client. No Postgres required.
