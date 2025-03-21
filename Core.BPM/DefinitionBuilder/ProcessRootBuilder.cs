using Core.BPM.Configuration;
using Core.BPM.DefinitionBuilder.Interfaces;
using Core.BPM.Evaluators.Factory;
using Core.BPM.Nodes;
using MediatR;

namespace Core.BPM.DefinitionBuilder;

public class ProcessRootBuilder<TProcess>(INodeEvaluatorFactory evaluatorFactory) : IProcessBuilder<TProcess>
    where TProcess : Aggregate
{
    public IProcessNodeInitialBuilder<TProcess> StartWith<TCommand>() where TCommand : IBaseRequest
    {
        var node = new Node(typeof(TCommand), typeof(TProcess), evaluatorFactory);
        var processInst = new BProcess(typeof(TProcess), node);
        BProcessGraphConfiguration.AddProcess(processInst);
        var builder = new ProcessBuilder<TProcess>(node, processInst, evaluatorFactory);
        return builder;
    }

    public IProcessNodeInitialBuilder<TProcess> StartWithAnyTime<TCommand>() where TCommand : IBaseRequest
    {
        var node = new AnyTimeNode(typeof(TCommand), typeof(TProcess), evaluatorFactory);
        var processInst = new BProcess(typeof(TProcess), node);
        BProcessGraphConfiguration.AddProcess(processInst);
        var builder = new ProcessBuilder<TProcess>(node, processInst, evaluatorFactory);
        return builder;
    }
}

public class ProcessConfig<T>(BProcess process) where T : Aggregate;