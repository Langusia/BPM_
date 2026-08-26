using System.Reflection;
using System.Security.Claims;
using BPM.Contracts;
using BPM.Core.Application;

namespace BPM.Mcp;

/// <summary>
/// Configuration surface for <c>UseMcp(...)</c>. Everything registered here
/// lands in the engine's <see cref="BpmAgentOptions"/> — the same options a
/// future UI adapter would populate through its own registration surface.
/// </summary>
public sealed class BpmMcpBuilder
{
    internal List<Action<BpmAgentOptions>> ConfigureActions { get; } = [];

    /// <summary>
    /// Global default identity mapping: how the authenticated principal becomes
    /// your identity object. Applies to every command carrying a
    /// <typeparamref name="TIdentity"/> property unless a more specific
    /// registration exists.
    /// </summary>
    public BpmMcpBuilder WithIdentity<TIdentity>(Func<ClaimsPrincipal, TIdentity> map)
        where TIdentity : class =>
        WithIdentity<TIdentity>((user, _) => Task.FromResult(map(user)));

    /// <summary>Async global identity mapping with service access for enrichment lookups.</summary>
    public BpmMcpBuilder WithIdentity<TIdentity>(Func<ClaimsPrincipal, IServiceProvider, Task<TIdentity>> map)
        where TIdentity : class
    {
        ConfigureActions.Add(o => o.Identity.RegisterGlobal(
            typeof(TIdentity),
            async (user, sp) => await map(user, sp)));
        return this;
    }

    /// <summary>
    /// Per-command (or per-marker-interface) identity override. Resolution
    /// order at execute time: exact command type → base interface → global default.
    /// </summary>
    public BpmMcpBuilder WithIdentityFor<TCommand, TIdentity>(
        Func<ClaimsPrincipal, IServiceProvider, Task<TIdentity>> map)
        where TIdentity : class
    {
        ConfigureActions.Add(o => o.Identity.RegisterFor(
            typeof(TCommand),
            typeof(TIdentity),
            async (user, sp) => await map(user, sp)));
        return this;
    }

    /// <summary>
    /// Per-command identity override with the identity type inferred at runtime
    /// from the resolved object. Prefer the two-generic overload when the field
    /// type must be classified as server-populated without a global registration.
    /// </summary>
    public BpmMcpBuilder WithIdentityFor<TCommand>(
        Func<ClaimsPrincipal, IServiceProvider, Task<object?>> map)
    {
        ConfigureActions.Add(o => o.Identity.RegisterFor(
            typeof(TCommand),
            typeof(object),
            (user, sp) => map(user, sp)));
        return this;
    }

    /// <summary>Scans an assembly for <see cref="IAgentSpec{TCommand}"/> implementations.</summary>
    public BpmMcpBuilder WithAgentSpecsFromAssembly(Assembly assembly)
    {
        ConfigureActions.Add(o => o.Specs.AddFromAssembly(assembly));
        return this;
    }

    /// <summary>Policy for commands with no attribute and no fluent spec. Default: RequiresApproval.</summary>
    public BpmMcpBuilder WithDefaultPolicy(ExecutionPolicy policy)
    {
        ConfigureActions.Add(o => o.DefaultPolicy = policy);
        return this;
    }
}
