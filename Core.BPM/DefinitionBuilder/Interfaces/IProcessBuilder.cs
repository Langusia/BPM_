using MediatR;

namespace Core.BPM.DefinitionBuilder.Interfaces;

public interface IProcessBuilder<TProcess> : IProcessBuilder where TProcess : Aggregate
{
    IProcessNodeInitialBuilder<TProcess> StartWith<TCommand>() where TCommand : IBaseRequest;
    IProcessNodeInitialBuilder<TProcess> StartWithAnyTime<TCommand>() where TCommand : IBaseRequest;
}

public interface IProcessBuilder
{
}