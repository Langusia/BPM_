using Core.BPM.Interfaces;
using Core.BPM.Interfaces.Builder;

namespace Core.BPM;

public class NodeBuilder(INode rootNode, BProcess process) : INodeBuilderBuilder //IInnerNodeBuilder, IOuterNodeBuilderBuilder
{
    private INode _rootNode = rootNode;
    private INode _currentNode = rootNode;

    public BProcess GetProcess() => process;
    public INode GetCurrent() => _currentNode;
    public INode GetRoot() => _rootNode;

    public INode SetRoot(INode node)
    {
        _rootNode = node;
        return _rootNode;
    }

    public INode SetCurrent(INode node)
    {
        _currentNode = node;
        return _currentNode;
    }

    INodeBuilderBuilder INodeBuilderBuilder.Continue<TCommand>(Action<INodeBuilderBuilder>? configure)
    {
        Continue(typeof(TCommand), configure);
        return this;
    }

    private void Continue(Type command, Action<INodeBuilderBuilder>? configure)
    {
        var node = new Node(command, process.ProcessType);

        if (command.Name == "PhoneChangeComplete")
        {
            var a = 1;
        }

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