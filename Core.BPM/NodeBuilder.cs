using Core.BPM.Interfaces;
using Core.BPM.Interfaces.Builder;

namespace Core.BPM;

public class NodeBuilder(INode rootNode, BProcess process) : IExtendableNodeBuilder
{
    private INode _rootNode = rootNode;
    private INode _currentNode = rootNode;

    public BProcess GetProcess() => process;
    public INode GetCurrent() => _currentNode;
    public INode GetRoot() => _rootNode;


    public IExtendableNodeBuilder Continue<TCommand>(Func<NodeBuilder, IExtendableNodeBuilder>? configure = null)
    {
        var node = new Node(typeof(TCommand), process.ProcessType);

        _rootNode.AddNextStepToTail(node);
        node.AddPrevStep(_currentNode);

        if (configure is not null)
        {
            var nextNodeBuilder = new NodeBuilder(node, process);
            var s = configure?.Invoke(nextNodeBuilder);
        }

        _rootNode = _currentNode;
        _currentNode = node;

        return this;
    }
}