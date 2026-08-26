using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BPM.Core.Application.Identity;

/// <summary>
/// Maps the authenticated principal to the consuming app's identity object(s).
/// Identity always comes from the transport-authenticated principal, never from
/// agent-supplied arguments. Resolution order for a command:
/// exact command type → base interface registration → global default.
/// </summary>
public sealed class BpmIdentityOptions
{
    private readonly Dictionary<Type, IdentityRegistration> _byCommandOrInterface = new();
    private IdentityRegistration? _global;
    private readonly HashSet<Type> _identityTypes = [];

    /// <summary>All identity CLR types seen in any registration; properties of these types are server-populated by convention.</summary>
    public IReadOnlyCollection<Type> IdentityTypes => _identityTypes;

    public void RegisterGlobal(Type identityType, IdentityResolver resolver)
    {
        _global = new IdentityRegistration(identityType, resolver);
        _identityTypes.Add(identityType);
    }

    /// <summary>Registers a resolver for an exact command type or a shared command interface.</summary>
    public void RegisterFor(Type commandOrInterfaceType, Type identityType, IdentityResolver resolver)
    {
        _byCommandOrInterface[commandOrInterfaceType] = new IdentityRegistration(identityType, resolver);
        _identityTypes.Add(identityType);
    }

    public IdentityRegistration? Resolve(Type commandType)
    {
        if (_byCommandOrInterface.TryGetValue(commandType, out var exact))
            return exact;

        var byInterface = commandType.GetInterfaces()
            .Where(_byCommandOrInterface.ContainsKey)
            .Select(i => _byCommandOrInterface[i])
            .FirstOrDefault();

        return byInterface ?? _global;
    }
}

/// <summary>Builds the identity object for a command from the authenticated principal.</summary>
public delegate Task<object?> IdentityResolver(ClaimsPrincipal principal, IServiceProvider services);

public sealed record IdentityRegistration(Type IdentityType, IdentityResolver Resolver);
