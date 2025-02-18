using Core.BPM.AggregateConditions;
using Core.BPM.DefinitionBuilder.Interfaces;
using Core.BPM.Evaluators.Factory;
using Core.BPM.Interfaces;
using Core.BPM.Nodes;

namespace Core.BPM.DefinitionBuilder;

public class GroupNodeBuilder<TProcess> : BaseNodeDefinition, IGroupBuilder<TProcess> where TProcess : Aggregate
{
    private List<INode> _memberNodes;
    private readonly BProcess _process;
    private readonly GroupNode _groupNode;
    private readonly INodeEvaluatorFactory _nodeEvaluatorFactory;
    private List<IProcessNodeModifiableBuilder<TProcess>> _builders = [];

    public GroupNodeBuilder(BProcess process, INode rootNode, GroupNode groupNode, INodeEvaluatorFactory nodeEvaluatorFactory) : base(rootNode, process)
    {
        _process = process;
        _groupNode = groupNode;
        _nodeEvaluatorFactory = nodeEvaluatorFactory;
    }


    public void AddStep<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null)
    {
        var node = new Node(typeof(TCommand), _process.ProcessType, _nodeEvaluatorFactory);
        _groupNode.SubRootNodes.Add(node);
        var builder = new ProcessNodeBuilder<TProcess>(node, _process, _nodeEvaluatorFactory);
        if (configure is not null)
        {
            var configured = configure?.Invoke(builder);
            _builders.Add(configured);
        }
        else
        {
            _builders.Add(builder);
        }
    }

    public void AddAnyTime<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null)
    {
        var node = new AnyTimeNode(typeof(TCommand), _process.ProcessType, _nodeEvaluatorFactory);
        _groupNode.SubRootNodes.Add(node);
        var builder = new ProcessNodeBuilder<TProcess>(node, _process, _nodeEvaluatorFactory);
        if (configure is not null)
        {
            var configured = configure?.Invoke(builder);
            _builders.Add(configured);
        }
        else
        {
            _builders.Add(builder);
        }
    }

    public void EndGroup()
    {
        var distSet = new List<INode>();
        var res = new List<INode>();
        var allNodes = new List<INode>();
        foreach (var builder in _builders)
        {
            IterateConfiguredProcessRoot(((ProcessNodeBuilder<TProcess>)builder).CurrentBranchInstances, res, distSet, allNodes);
        }

        _memberNodes = allNodes.Distinct().ToList();
        _groupNode.SetAllMembers(_memberNodes);
    }
}