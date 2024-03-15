using Credo.Core.Shared.Library;
using MediatR;

namespace Core.BPM.MediatR.Mediator;

public record CredoEvent<T> : CredoEvent where T : IRequest<Result>
{
    protected CredoEvent() : base(typeof(T))
    {
    }
}

public record CredoEvent
{
    protected CredoEvent(Type commandType)
    {
        CommandType = commandType;
    }

    public Type CommandType { get; init; }
}