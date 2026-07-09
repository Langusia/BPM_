using BPM.Core.Application;
using Microsoft.Extensions.DependencyInjection;

namespace BPM.Mcp;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Exposes this service's BPM processes as an MCP server. Wires the MCP
    /// server SDK with a stateless streamable-HTTP transport (every tool call
    /// runs against its own authenticated HttpContext) and registers the
    /// generic bpm_* dispatch tools. Map the endpoint with
    /// <c>app.MapMcp("/mcp")</c> and protect it with the host's normal auth
    /// (<c>.RequireAuthorization()</c>); this package never validates tokens itself.
    /// </summary>
    public static IServiceCollection UseMcp(
        this IServiceCollection services, Action<BpmMcpBuilder>? configure = null)
    {
        var builder = new BpmMcpBuilder();
        configure?.Invoke(builder);

        foreach (var action in builder.ConfigureActions)
            services.Configure<BpmAgentOptions>(action);

        services.AddHttpContextAccessor();

        services.AddMcpServer()
            .WithHttpTransport(o => o.Stateless = true)
            .WithTools<BpmMcpTools>();

        return services;
    }
}
