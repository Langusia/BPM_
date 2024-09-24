using Core.BPM.Interfaces;
using Core.BPM.Nodes;

namespace Core.BPM.DefinitionBuilder;

public class NodeBuilder(INode rootNode, BProcess process) : INodeDefinitionBuilder //IInnerNodeBuilder, IOuterNodeBuilderBuilder
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

    INodeDefinitionBuilder INodeDefinitionBuilder.Continue<TCommand>(Action<INodeDefinitionBuilder>? configure)
    {
        Continue(new Node(typeof(TCommand), GetProcess().ProcessType), configure);
        return this;
    }

    public INodeDefinitionBuilder ContinueAnyTime<TCommand>(Action<INodeDefinitionBuilder>? configure = null)
    {
        Continue(new AnyTimeNode(typeof(TCommand), GetProcess().ProcessType), configure);
        return this;
    }

    public INodeDefinitionBuilder ContinueOptional<TCommand>(Action<INodeDefinitionBuilder>? configure = null)
    {
        Continue(new OptionalNode(typeof(TCommand), GetProcess().ProcessType), configure);
        return this;
    }

    private void Continue(INode node, Action<INodeDefinitionBuilder>? configure)
    {
        //var node = new Node(command, process.ProcessType);

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