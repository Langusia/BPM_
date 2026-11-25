namespace BPM.Contracts;

/// <summary>
/// Companion spec supplying agent-grade metadata for a command. Colocate with
/// the command and register via assembly scan
/// (<c>WithAgentSpecsFromAssembly(...)</c>). Fluent spec output takes precedence
/// over attributes, which take precedence over reflection defaults.
/// </summary>
public interface IAgentSpec<TCommand>
{
    void Configure(AgentSpecBuilder<TCommand> spec);
}
