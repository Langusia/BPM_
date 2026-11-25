namespace BPM.Contracts;

/// <summary>
/// Server-enforced execution tier for a command when invoked by an AI agent
/// (or any non-human adapter). Enforced in the execution path by the engine,
/// never by prompt instructions.
/// </summary>
public enum ExecutionPolicy
{
    /// <summary>The agent may execute the command directly.</summary>
    Autonomous,

    /// <summary>
    /// The agent may propose the command, but execution is physically gated:
    /// the engine returns a structured "approval required" result instead of dispatching.
    /// This is the default for unannotated commands.
    /// </summary>
    RequiresApproval,

    /// <summary>The command is not executable through an agent adapter at all.</summary>
    HumanOnly
}
