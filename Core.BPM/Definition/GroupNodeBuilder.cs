using System;
using System.Collections.Generic;
using System.Linq;
using Core.BPM.Definition.Interfaces;
using Core.BPM.Nodes;
using Core.BPM.Nodes.Evaluation;
using Core.BPM.Process;

namespace Core.BPM.Definition;

public class GroupNodeBuilder<TProcess> : NodeBuilderBase, IGroupBuilder<TProcess> where TProcess : Aggregate
{
    private List<INode> _memberNodes;
    private readonly BProcess _process;
    private int _depthCounter;
    private readonly GroupNode _groupNode;
    private readonly INodeEvaluatorFactory _nodeEvaluatorFactory;
    private List<IProcessNodeModifiableBuilder<TProcess>> _builders = [];

    public GroupNodeBuilder(BProcess process, INode rootNode, GroupNode groupNode, INodeEvaluatorFactory nodeEvaluatorFactory, int depthCounter) : base(rootNode, process)
    {
        _process = process;
        _groupNode = groupNode;
        _nodeEvaluatorFactory = nodeEvaluatorFactory;
        _depthCounter = depthCounter;
    }


    public void AddStep<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null)
    {
        var node = new Node(typeof(TCommand), _process.ProcessType, _nodeEvaluatorFactory);
        _groupNode.SubRootNodes.Add(node);
        var builder = new ProcessBuilder<TProcess>(node, _process, _nodeEvaluatorFactory);
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
        var builder = new ProcessBuilder<TProcess>(node, _process, _nodeEvaluatorFactory);
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
        var res = new List<INode>();
        var allNodes = new List<INode>();
        int stepCounter = 1;

        HashSet<INode> visited = [];
        foreach (var builder in _builders)
        {
            IterateConfiguredProcessRoot(((ProcessBuilder<TProcess>)builder).CurrentBranchInstances, res, visited, (_depthCounter + stepCounter) * 100);
            stepCounter += 1;
        }

        _memberNodes = allNodes.Distinct().ToList();
        _groupNode.SetAllMembers(_memberNodes);
    }
}
