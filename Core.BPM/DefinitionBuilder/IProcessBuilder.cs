using Core.BPM.Configuration;
using Core.BPM.Nodes;
using MediatR;

namespace Core.BPM.DefinitionBuilder;

public interface IProcessBuilder<out TProcess> where TProcess : Aggregate
{
    IProcessNodeInitialBuilder<TProcess> StartWith<TCommand>() where TCommand : IBaseRequest;
}