using MediatR;

namespace Core.BPM.DefinitionBuilder;

public interface IProcessBuilder<TProcess> where TProcess : Aggregate
{
    IProcessNodeInitialBuilder<TProcess> StartWith<TCommand>() where TCommand : IBaseRequest;
}