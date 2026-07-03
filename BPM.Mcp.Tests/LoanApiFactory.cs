using System.Collections.Concurrent;
using BPM.Core.Application.Persistence;
using BPM.Core.Persistence;
using BPM.Core.Process;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace BPM.Mcp.Tests;

/// <summary>
/// Hosts the Loan.Api sample with in-memory persistence swapped in behind the
/// engine's ports. Everything else — MCP transport, JWT auth, MediatR pipeline,
/// identity population, policy gate — runs for real.
/// </summary>
public sealed class LoanApiFactory : WebApplicationFactory<Program>
{
    public InMemoryEventStore EventStore { get; } = new();
    public CaptureLogSink Logs { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddSingleton(EventStore);
            services.RemoveAll<IBpmRepository>();
            services.RemoveAll<BpmRepository>();
            services.RemoveAll<IProcessStore>();
            services.RemoveAll<IProcessInstanceStore>();
            services.AddScoped<IBpmRepository, InMemoryBpmRepository>();
            services.AddScoped<IProcessStore, InMemoryProcessStore>();
            services.AddScoped<IProcessInstanceStore, InMemoryProcessInstanceStore>();
        });
        builder.ConfigureLogging(logging => logging.AddProvider(new CaptureLoggerProvider(Logs)));
    }
}

public sealed class CaptureLogSink
{
    public ConcurrentQueue<string> Lines { get; } = new();
}

public sealed class CaptureLoggerProvider(CaptureLogSink sink) : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new CaptureLogger(sink);
    public void Dispose() { }

    private sealed class CaptureLogger(CaptureLogSink sink) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => sink.Lines.Enqueue(formatter(state, exception));
    }
}
