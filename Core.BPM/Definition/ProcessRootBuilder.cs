using Core.BPM.Configuration;
using Core.BPM.Definition.Interfaces;
using Core.BPM.Nodes;
using Core.BPM.Nodes.Evaluation;
using Core.BPM.Process;
using MediatR;

namespace Core.BPM.Definition;

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
