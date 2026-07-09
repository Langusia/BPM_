using System;

namespace BPM.Contracts;

/// <summary>
/// Implemented by the value an initial command returns so the MCP adapter can recover
/// the id of the process that was just started. After dispatching an initial command,
/// the engine unwraps a result-style wrapper (a <c>Value</c> property, e.g. Result&lt;T&gt;)
/// if present and reads <see cref="ProcessId"/> from the payload. A command that instead
/// returns a bare <see cref="Guid"/> also works, without implementing this.
/// </summary>
public interface IProcessStartResult
{
    Guid ProcessId { get; }
}
