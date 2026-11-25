namespace BPM.Contracts;

/// <summary>
/// Marker for commands that carry a server-populated identity of type
/// <typeparamref name="TIdentity"/>. The engine populates the property whose
/// type is <typeparamref name="TIdentity"/> from the authenticated transport;
/// identity resolvers registered for this interface apply to all commands
/// implementing it (resolution order: exact command type → this interface → global default).
/// </summary>
public interface IAuthenticatedRequest<TIdentity>;
