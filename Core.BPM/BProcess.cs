using Core.BPM.Interfaces;
using MediatR;

namespace Core.BPM;

public class BProcess(Type processType, INode rootNode)
{
    public readonly Type ProcessType = processType;
    public readonly INode RootNode = rootNode;
}

public class BProcessAggregateState(Aggregate aggregate, Type processType, BProcess? processConfig) : BProcess(processType, processConfig.RootNode)
{
    public bool ValidatePathFor<TCommand>() where TCommand : IRequest
    {
        return true;
    }
}