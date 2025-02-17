using Core.BPM.Configuration;
using Core.BPM.DefinitionBuilder.Interfaces;
using Core.BPM.Evaluators.Factory;
using Core.BPM.Nodes;
using MediatR;

namespace Core.BPM.DefinitionBuilder;

public class ProcessBuilder<TProcess>(INodeEvaluatorFactory evaluatorFactory) : IProcessBuilder<TProcess>
    where TProcess : Aggregate
{
    public IProcessNodeInitialBuilder<TProcess> StartWith<TCommand>() where TCommand : IBaseRequest
    {
        var node = new Node(typeof(TCommand), typeof(TProcess), evaluatorFactory);
        var processInst = new BProcess(typeof(TProcess), node);
        BProcessGraphConfiguration.AddProcess(processInst);
        var builder = new ProcessNodeBuilder<TProcess>(node, processInst, evaluatorFactory);
        return builder;
    }
}

public class ProcessConfig<T> where T : Aggregate;