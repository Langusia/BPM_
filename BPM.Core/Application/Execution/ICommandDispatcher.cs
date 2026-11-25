using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace BPM.Core.Application.Execution;

/// <summary>
/// Dispatches a bound command instance through the host's normal execution
/// pipeline. There is exactly one implementation — MediatR — so agent-driven
/// dispatch runs the same pipeline behaviors (auth, validation, logging) as
/// every other caller. No second dispatch mechanism.
/// </summary>
public interface ICommandDispatcher
{
    Task<object?> DispatchAsync(object command, CancellationToken ct);
}

internal sealed class MediatRCommandDispatcher(IMediator mediator) : ICommandDispatcher
{
    public Task<object?> DispatchAsync(object command, CancellationToken ct) =>
        mediator.Send(command, ct);
}
