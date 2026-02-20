using BPM.Core.Configuration;
using BPM.Core.Definition.Interfaces;
using BPM.Core.Nodes;
using BPM.Core.Nodes.Evaluation;
using BPM.Core.Process;
using MediatR;

namespace BPM.Core.Definition;

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
