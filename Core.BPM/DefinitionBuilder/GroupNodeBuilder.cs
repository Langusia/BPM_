using Core.BPM.AggregateConditions;
using Core.BPM.Interfaces;
using Core.BPM.Nodes;

namespace Core.BPM.DefinitionBuilder;

public class GroupNodeBuilder<TProcess> : BaseNodeDefinition, IGroupBuilder<TProcess> where TProcess : Aggregate
{
    private readonly BProcess _process;
    private readonly GroupNode _groupNode;
    private readonly List<INode> _groupedNodes = [];
    private List<IProcessNodeModifiableBuilder<TProcess>> _builders = [];

    public INode GetCurrentNode() => _groupNode;
    public BProcess GetProcess() => _process;

    public GroupNodeBuilder(BProcess process, INode rootNode, string groupId, GroupNode groupNode) : base(rootNode, process)
    {
        _process = process;
        _groupNode = groupNode;
    }


    public void AddStep<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null)
    {
        var node = new Node(typeof(TCommand), _process.ProcessType);
        _groupedNodes.Add(node);
        var configured = configure?.Invoke(new ProcessNodeBuilder<TProcess>(node, _process));
        _builders.Add(configured);
    }

    public void AddAnyTime<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null)
    {
        var node = new AnyTimeNode(typeof(TCommand), _process.ProcessType);
        _groupedNodes.Add(node);
        var configured = configure?.Invoke(new ProcessNodeBuilder<TProcess>(node, _process));
        _builders.Add(configured);
    }

    public void EndGroup()
    {
        var distSet = new List<INode>();
        var res = new List<INode>();
        foreach (var builder in _builders)
        {
            IterateConfiguredProcessRoot(((ProcessNodeBuilder<TProcess>)builder).CurrentBranchInstances, res, distSet);
        }

        var processNodeBuilder = new ProcessNodeBuilder<TProcess>(_groupNode, _process);
    }
}