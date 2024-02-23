using Core.BPM.Interfaces;
using Credo.Core.Shared.Library;
using MediatR;

namespace Core.BPM.MediatR;

public class CredoBpmNode<TProcess, TCommand> : BpmNode<TProcess, TCommand>
    where TProcess : IProcess where TCommand : IRequest<Result>
{
}