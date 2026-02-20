using System;
using System.Collections.Generic;
using BPM.Core.Definition.Interfaces;
using BPM.Core.Nodes;
using BPM.Core.Nodes.Evaluation;
using BPM.Core.Process;

namespace BPM.Core.Definition;

public class ConditionalBuilder<TProcess>(
    INode firstNode,
    BProcess process,
    ConditionalNode nodeToConfigure,
    INodeEvaluatorFactory nodeEvaluatorFactory,
    int depthCounter)
    : ProcessBuilder<TProcess>(firstNode, process, nodeEvaluatorFactory, depthCounter), IConditionalModifiableBuilder<TProcess> where TProcess : Aggregate
{
    public void SetIfNode(List<INode> currentInstances)
    {
        HashSet<INode> visited = [];
        var r = new List<INode>();

        IterateConfiguredProcessRoot(currentInstances, r, visited, depthCounter * 1000);
        nodeToConfigure.IfNodeRoots = r;
    }

    public IProcessNodeModifiableBuilder<TProcess> Else(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure)
    {
        var elseBuilder = new ProcessBuilder<TProcess>(null, process, nodeEvaluatorFactory);
        var configuredBranch = configure.Invoke(elseBuilder);
        return ElseCore((NodeBuilderBase)configuredBranch);
    }

    public IProcessNodeModifiableBuilder<TProcess> Else(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeNonModifiableBuilder<TProcess>> configure)
    {
        var elseBuilder = new ProcessBuilder<TProcess>(null, process, nodeEvaluatorFactory);
        var configuredBranch = configure.Invoke(elseBuilder);
        return ElseCore((NodeBuilderBase)configuredBranch);
    }

    private IProcessNodeModifiableBuilder<TProcess> ElseCore(NodeBuilderBase configuredBranch)
    {
        HashSet<INode> visited = [];
        var r = new List<INode>();

        var elseNodes = configuredBranch.CurrentBranchInstances;
        IterateConfiguredProcessRoot(elseNodes, r, visited, -depthCounter * 1000);

        nodeToConfigure.ElseNodeRoots = r;
        return this;
    }
}
