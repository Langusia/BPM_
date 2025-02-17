using Core.BPM.Configuration;
using Core.BPM.DefinitionBuilder.Interfaces;
using Core.BPM.Evaluators.Factory;
using Core.BPM.Nodes;
using MediatR;

namespace Core.BPM.DefinitionBuilder;

public class ProcessBuilder<TProcess> : IProcessBuilder<TProcess> where TProcess : Aggregate
{
    private readonly INodeEvaluatorFactory _evaluatorFactory;

    public ProcessBuilder(INodeEvaluatorFactory evaluatorFactory)
    {
        _evaluatorFactory = evaluatorFactory;
    }

    public IProcessNodeInitialBuilder<TProcess> StartWith<TCommand>() where TCommand : IBaseRequest
    {
        var node = new Node(typeof(TCommand), typeof(TProcess), _evaluatorFactory);
        var processInst = new BProcess(typeof(TProcess), node);
        BProcessGraphConfiguration.AddProcess(processInst);
        var builder = new ProcessNodeBuilder<TProcess>(node, processInst, _evaluatorFactory);
        return builder;
    }
}

public class ProcessConfig<T> where T : Aggregate;