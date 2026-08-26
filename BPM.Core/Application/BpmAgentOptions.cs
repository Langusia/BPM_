using BPM.Contracts;
using BPM.Core.Application.Identity;
using BPM.Core.Application.Metadata;

namespace BPM.Core.Application;

/// <summary>
/// Agent-facing configuration shared by every adapter over the application
/// layer. Adapters (BPM.Mcp today, UI 2.0 later) populate this via their own
/// registration surface; the engine consumes it for schema projection,
/// identity population and the policy gate.
/// </summary>
public sealed class BpmAgentOptions
{
    public BpmIdentityOptions Identity { get; } = new();

    public AgentSpecRegistry Specs { get; } = new();

    /// <summary>Policy applied to commands with no attribute and no fluent spec.</summary>
    public ExecutionPolicy DefaultPolicy { get; set; } = ExecutionPolicy.RequiresApproval;
}
