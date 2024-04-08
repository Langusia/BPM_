using Core.BPM.Interfaces;
using Core.BPM.Interfaces.Builder;

namespace Core.BPM;

public class NodeBuilder(INode rootNode, BProcess process) : IInnerNodeBuilder, IOuterNodeBuilderBuilder
{
    private INode _rootNode = rootNode;
    private INode _currentNode = rootNode;

    public BProcess GetProcess() => process;
    public INode GetCurrent() => _currentNode;
    public INode GetRoot() => _rootNode;

    IOuterNodeBuilderBuilder IOuterNodeBuilderBuilder.Continue<TCommand>(Action<IInnerNodeBuilder>? configure)
    {
        Continue(typeof(TCommand), configure);
        return this;
    }

    IInnerNodeBuilder IInnerNodeBuilder.Continue<TCommand>(Action<IInnerNodeBuilder>? configure)
    {
        Continue(typeof(TCommand), configure);
        return this;
    }

    private void Continue(Type command, Action<IInnerNodeBuilder>? configure)
    {
        var node = new Node(command, process.ProcessType);

        _rootNode.AddNextStepToTail(node);
        node.AddPrevStep(_currentNode);

        if (configure is not null)
        {
            var nextNodeBuilder = new NodeBuilder(node, process);
            configure?.Invoke(nextNodeBuilder);
        }

        _rootNode = _currentNode;
        _currentNode = node;
    }
}