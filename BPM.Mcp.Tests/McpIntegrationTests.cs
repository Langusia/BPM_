using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BPM.Core.Configuration;
using Loan.Api;
using Loan.Contracts;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Xunit;

namespace BPM.Mcp.Tests;

/// <summary>
/// Drives the sample host end-to-end through the official MCP client SDK over
/// streamable HTTP with a real JWT. One test class: the process graph lives in
/// static engine state, so the host is built exactly once.
/// </summary>
public class McpIntegrationTests : IClassFixture<LoanApiFactory>
{
    private readonly LoanApiFactory _factory;

    public McpIntegrationTests(LoanApiFactory factory) => _factory = factory;

    private async Task<McpClient> CreateMcpClientAsync()
    {
        var token = await _factory.CreateClient().GetStringAsync("/dev-token");
        var httpClient = _factory.CreateDefaultClient();
        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(httpClient.BaseAddress!, "/mcp"),
            TransportMode = HttpTransportMode.StreamableHttp,
            AdditionalHeaders = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {token}"
            }
        }, httpClient, loggerFactory: null, ownsHttpClient: true);
        return await McpClient.CreateAsync(transport);
    }

    private static async Task<JsonElement> CallAsync(McpClient client, string tool, object? args = null)
    {
        var dictionary = args is null
            ? new Dictionary<string, object?>()
            : JsonSerializer.Deserialize<Dictionary<string, object?>>(JsonSerializer.Serialize(args))!;
        var result = await client.CallToolAsync(tool, dictionary);
        var text = Assert.IsType<TextContentBlock>(Assert.Single(result.Content)).Text;
        return JsonDocument.Parse(text).RootElement;
    }

    [Fact]
    public async Task ListTools_ExposesTheSevenGenericBpmTools()
    {
        await using var client = await CreateMcpClientAsync();

        var tools = await client.ListToolsAsync();
        var names = tools.Select(t => t.Name).ToHashSet();

        Assert.Superset(new HashSet<string>
        {
            "bpm_list_process_types",
            "bpm_start_process",
            "bpm_get_process",
            "bpm_get_next_steps",
            "bpm_get_command_schema",
            "bpm_execute_command",
            "bpm_get_history"
        }, names);
    }

    [Fact]
    public async Task Mcp_WithoutBearerToken_IsRejectedByHostAuth()
    {
        using var raw = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/mcp")
        {
            Content = new StringContent(
                """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}""",
                Encoding.UTF8, "application/json")
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

        var response = await raw.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CommandSchema_IsAProjection_NotTheRawRecord()
    {
        await using var client = await CreateMcpClientAsync();

        var schema = await CallAsync(client, "bpm_get_command_schema",
            new { commandName = "InitiateLoanApplication" });

        Assert.True(schema.GetProperty("ok").GetBoolean());
        var result = schema.GetProperty("result");
        Assert.Equal("autonomous", result.GetProperty("executionPolicy").GetString());
        Assert.True(result.GetProperty("isInitial").GetBoolean());

        var args = result.GetProperty("argumentsSchema");
        var properties = args.GetProperty("properties");

        // Agent inputs with constraints from the fluent spec.
        Assert.Equal(500, properties.GetProperty("amount").GetProperty("minimum").GetDouble());
        Assert.Equal(50_000, properties.GetProperty("amount").GetProperty("maximum").GetDouble());
        var purposeValues = properties.GetProperty("purpose").GetProperty("enum")
            .EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("CarPurchase", purposeValues);

        // Identity is server-populated: never in the schema.
        Assert.False(properties.TryGetProperty("userContext", out _));

        var required = args.GetProperty("required").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Contains("amount", required);
        Assert.Contains("termMonths", required);
    }

    [Fact]
    public async Task FullLoanFlow_IdentityStamping_DerivedPricing_ApprovalGate_HumanOnlyGate()
    {
        await using var client = await CreateMcpClientAsync();
        var behaviorCountBefore =
            CommandAuditBehavior<InitiateLoanApplication, Guid>.InvocationCount;

        // 1. Start the process. The agent tries to spoof identity and the
        //    derived rate; both must be discarded.
        var started = await CallAsync(client, "bpm_start_process", new
        {
            processType = "LoanApplication",
            commandName = "InitiateLoanApplication",
            argsJson = """
                       {"amount": 5000, "termMonths": 24, "purpose": "CarPurchase",
                        "userContext": {"userId": "EVIL", "fullName": "Mallory", "email": "m@evil"}}
                       """
        });

        Assert.True(started.GetProperty("ok").GetBoolean());
        var startResult = started.GetProperty("result");
        var processId = startResult.GetProperty("processId").GetGuid();

        // Identity came from the JWT, not from argsJson.
        var state = startResult.GetProperty("state");
        Assert.Equal("cust-1001", state.GetProperty("customerId").GetString());
        Assert.Equal("Nino Beridze", state.GetProperty("customerName").GetString());

        // The discarded spoof was logged.
        Assert.Contains(_factory.Logs.Lines, l => l.Contains("Discarded") && l.Contains("UserContext"));

        // The MediatR pipeline behavior fired on the MCP path.
        Assert.True(CommandAuditBehavior<InitiateLoanApplication, Guid>.InvocationCount > behaviorCountBefore);

        // Execute + next steps arrive in the same response.
        Assert.Equal("SubmitCollateral",
            startResult.GetProperty("nextSteps")[0].GetProperty("name").GetString());

        // 2. Submit collateral.
        var collateral = await CallAsync(client, "bpm_execute_command", new
        {
            processId,
            commandName = "SubmitCollateral",
            argsJson = """{"vehicleVin": "1HGCM82633A004352", "vehicleYear": 2015, "estimatedValue": 9000}"""
        });
        Assert.True(collateral.GetProperty("ok").GetBoolean());

        // 3. Price the loan — the rate is computed server-side.
        var priced = await CallAsync(client, "bpm_execute_command", new
        {
            processId,
            commandName = "PriceLoan",
            argsJson = """{"effectiveInterestRate": 0.01}""" // derived: must be ignored
        });
        Assert.True(priced.GetProperty("ok").GetBoolean());
        var rate = priced.GetProperty("result").GetProperty("state")
            .GetProperty("effectiveInterestRate").GetDecimal();
        Assert.True(rate > 18m, $"pricing logic should own the rate, got {rate}");

        // 4. SignLoanContract requires approval: physically gated, not executed.
        var signAttempt = await CallAsync(client, "bpm_execute_command", new
        {
            processId,
            commandName = "SignLoanContract",
            argsJson = "{}"
        });
        Assert.False(signAttempt.GetProperty("ok").GetBoolean());
        Assert.Equal("approval_required",
            signAttempt.GetProperty("error").GetProperty("code").GetString());

        // Still pending: the contract was NOT signed.
        var steps = await CallAsync(client, "bpm_get_next_steps", new { processId });
        Assert.Equal("SignLoanContract",
            steps.GetProperty("result")[0].GetProperty("name").GetString());

        // 5. A human signs out-of-band (seeded directly into the store), then
        //    the agent tries to disburse: human-only, never executable via MCP.
        var signNodeLevel = BProcessGraphConfiguration.GetConfig(nameof(LoanApplication))!
            .RootNode.GetAllNodes().First(n => n.CommandType == typeof(SignLoanContract)).NodeLevel;
        _factory.EventStore.Append(processId, nameof(LoanApplication),
            [new LoanContractSigned("Bank Officer", "approved on paper") { NodeId = signNodeLevel }]);

        var disburseAttempt = await CallAsync(client, "bpm_execute_command", new
        {
            processId,
            commandName = "DisburseLoan",
            argsJson = "{}"
        });
        Assert.False(disburseAttempt.GetProperty("ok").GetBoolean());
        Assert.Equal("policy_human_only",
            disburseAttempt.GetProperty("error").GetProperty("code").GetString());

        // 6. The event timeline shows exactly what happened.
        var history = await CallAsync(client, "bpm_get_history", new { processId });
        var eventTypes = history.GetProperty("result").EnumerateArray()
            .Select(h => h.GetProperty("eventType").GetString()).ToList();
        Assert.Equal(
            ["LoanApplicationInitiated", "CollateralSubmitted", "LoanPriced", "LoanContractSigned"],
            eventTypes);
    }

    [Fact]
    public async Task ListProcessTypes_DescribesTheLoanProcessAndItsEntryCommand()
    {
        await using var client = await CreateMcpClientAsync();

        var listed = await CallAsync(client, "bpm_list_process_types");

        Assert.True(listed.GetProperty("ok").GetBoolean());
        var loan = listed.GetProperty("result").EnumerateArray()
            .Single(p => p.GetProperty("name").GetString() == "LoanApplication");
        Assert.Contains("loan", loan.GetProperty("description").GetString()!,
            StringComparison.OrdinalIgnoreCase);

        // Only entry commands are listed: the initial command is present ...
        var commands = loan.GetProperty("entryCommands").EnumerateArray().ToList();
        var initiate = commands.Single(c => c.GetProperty("name").GetString() == "InitiateLoanApplication");
        Assert.True(initiate.GetProperty("isInitial").GetBoolean());
        // ... and non-initial commands (e.g. DisburseLoan) are not surfaced here.
        Assert.DoesNotContain(commands, c => c.GetProperty("name").GetString() == "DisburseLoan");
    }

    [Fact]
    public async Task InvalidArguments_ProduceInstructiveStructuredErrors()
    {
        await using var client = await CreateMcpClientAsync();

        var result = await CallAsync(client, "bpm_start_process", new
        {
            processType = "LoanApplication",
            commandName = "InitiateLoanApplication",
            argsJson = """{"termMonths": 24}"""
        });

        Assert.False(result.GetProperty("ok").GetBoolean());
        var error = result.GetProperty("error");
        Assert.Equal("invalid_arguments", error.GetProperty("code").GetString());
        Assert.Contains("Amount", error.GetProperty("message").GetString()!);
        Assert.Contains("bpm_get_command_schema", error.GetProperty("hint").GetString()!);
    }
}
