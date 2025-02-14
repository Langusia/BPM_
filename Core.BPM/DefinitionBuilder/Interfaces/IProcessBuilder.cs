using MediatR;

namespace Core.BPM.DefinitionBuilder;

public interface IProcessBuilder<TProcess> : IProcessBuilder where TProcess : Aggregate
{
    IProcessNodeInitialBuilder<TProcess> StartWith<TCommand>() where TCommand : IBaseRequest;
}

public interface IProcessBuilder
{
}