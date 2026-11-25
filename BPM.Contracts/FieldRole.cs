namespace BPM.Contracts;

/// <summary>
/// Classification bucket for a command property. Every property of a command
/// lands in exactly one bucket; only <see cref="Input"/> fields are exposed
/// to agents in the projected schema.
/// </summary>
public enum FieldRole
{
    /// <summary>Supplied by the agent; appears in the projected schema.</summary>
    Input,

    /// <summary>
    /// Populated by the server from the authenticated transport (e.g. identity).
    /// Never in the schema; agent-supplied values are discarded and logged.
    /// </summary>
    ServerPopulated,

    /// <summary>Computed by server-side logic (e.g. pricing). Never in the schema.</summary>
    Derived
}
