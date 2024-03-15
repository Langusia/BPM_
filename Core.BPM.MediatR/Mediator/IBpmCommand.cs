using Credo.Core.Shared.Mediator;

namespace Core.BPM.MediatR.Mediator;

public interface IBpmCommand<TResponse> : IBpmRootCommand<TResponse>, IBpmCommand
{
}

public interface IBpmCommand
{
    public Guid ProcessId { get; init; }
}

public interface IBpmRootCommand<TResponse> : ICommand<TResponse>
{
}