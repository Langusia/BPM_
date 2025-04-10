using System;
using System.Collections.Generic;
using Core.BPM.DefinitionBuilder.Interfaces;
using Core.BPM.Evaluators.Factory;
using Core.BPM.Interfaces;
using Core.BPM.Nodes;

namespace Core.BPM.DefinitionBuilder;

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
        HashSet<INode> visited = [];
        var r = new List<INode>();

        var elseBuilder = new ProcessBuilder<TProcess>(null, process, nodeEvaluatorFactory);
        var configuredBranch = configure.Invoke(elseBuilder);
        var elseNodes = ((NodeBuilderBase)configuredBranch).CurrentBranchInstances;
        IterateConfiguredProcessRoot(elseNodes, r, visited, -depthCounter * 1000);

        nodeToConfigure.ElseNodeRoots = r;
        return this;
    }
}