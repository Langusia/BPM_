using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using BPM.Core.Application;
using BPM.Core.Application.Catalog;
using BPM.Core.Application.Execution;
using BPM.Core.Application.Metadata;
using BPM.Core.Application.Persistence;
using BPM.Core.Application.Projection;
using BPM.Core.Configuration;
using BPM.Core.Definition;
using BPM.Core.Nodes.Evaluation;
using BPM.Core.Persistence;
using BPM.Core.Process;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace BPM.Tests.Application;

/// <summary>
/// In-memory <see cref="IProcessInstanceStore"/>. Standing in for Marten in
/// application-layer tests doubles as proof that the persistence seam holds.
/// </summary>
public sealed class InMemoryInstanceStore : IProcessInstanceStore
{
    private readonly Dictionary<Guid, (string AggregateName, List<object> Events, DateTimeOffset Started)> _streams = new();

    public void Seed(Guid processId, string aggregateName, params object[] events)
    {
        if (!_streams.TryGetValue(processId, out var stream))
            _streams[processId] = stream = (aggregateName, new List<object>(), DateTimeOffset.UtcNow);
        stream.Events.AddRange(events);
    }

    public Task<ProcessInstanceSnapshot?> LoadAsync(Guid processId, CancellationToken ct)
    {
        if (!_streams.TryGetValue(processId, out var stream) || stream.Events.Count == 0)
            return Task.FromResult<ProcessInstanceSnapshot?>(null);

        var envelopes = stream.Events
            .Select((e, i) => new ProcessEventEnvelope(e.GetType().Name, e, i + 1, stream.Started))
            .ToList();
        return Task.FromResult<ProcessInstanceSnapshot?>(
            new ProcessInstanceSnapshot(processId, stream.AggregateName, envelopes, stream.Started));
    }
}

public sealed class FakeDispatcher : ICommandDispatcher
{
    public List<object> Dispatched { get; } = [];
    public Func<object, Task<object?>>? OnDispatch { get; set; }

    public Task<object?> DispatchAsync(object command, CancellationToken ct)
    {
        Dispatched.Add(command);
        return OnDispatch?.Invoke(command) ?? Task.FromResult<object?>(null);
    }
}

public sealed class CapturingLogger<T> : ILogger<T>
{
    public ConcurrentBag<string> Messages { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception));
}

/// <summary>
/// Base for tests that need registered process graphs. Resets the static graph
/// configuration around each test and wires the application layer against the
/// in-memory store.
/// </summary>
public abstract class GraphTestBase : IDisposable
{
    protected readonly INodeEvaluatorFactory EvaluatorFactory;
    protected readonly InMemoryInstanceStore Store = new();
    protected readonly FakeDispatcher Dispatcher = new();
    protected readonly CapturingLogger<AgentProcessService> Logger = new();
    protected readonly BpmAgentOptions Options = new();
    protected readonly ProcessRegistry Registry = new();

    protected GraphTestBase()
    {
        ClearProcesses();
        EvaluatorFactory = new NodeEvaluatorFactory(Substitute.For<IBpmRepository>(), new ReplayContext(Registry));
    }

    public void Dispose() => ClearProcesses();

    protected void BuildDefinition<T, TDefinition>()
        where T : Aggregate
        where TDefinition : BpmDefinition<T>, new()
    {
        new TDefinition().DefineProcess(new ProcessRootBuilder<T>(EvaluatorFactory));
        Registry.RegisterAggregate(typeof(T));
    }

    protected AgentProcessService CreateService(out ICommandCatalog catalog)
    {
        catalog = CommandCatalog.FromRegisteredProcesses();
        var resolver = new CommandMetadataResolver(Options.Specs, Options.Identity, Options.DefaultPolicy);
        return new AgentProcessService(
            catalog,
            Store,
            Dispatcher,
            resolver,
            new CommandSchemaProjector(resolver),
            Options,
            Registry,
            Substitute.For<IServiceProvider>(),
            Logger);
    }

    protected static ClaimsPrincipal AuthenticatedUser(string userId = "u-1", string name = "Test User") =>
        new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, name)
        ], authenticationType: "test"));

    /// <summary>Node level for a command in a registered graph, for seeding correctly stamped events.</summary>
    protected static int NodeLevelOf<TAggregate, TCommand>() =>
        BProcessGraphConfiguration.GetConfig(typeof(TAggregate).Name)!
            .RootNode.GetAllNodes()
            .First(n => n.CommandType == typeof(TCommand))
            .NodeLevel;

    private static void ClearProcesses()
    {
        var field = typeof(BProcessGraphConfiguration)
            .GetField("_processes", BindingFlags.Static | BindingFlags.NonPublic)!;
        field.SetValue(null, null);
    }
}
