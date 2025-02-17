using Core.BPM.DefinitionBuilder.Interfaces;
using Core.BPM.Evaluators.Factory;
using Core.BPM.Interfaces;
using Core.BPM.Nodes;

namespace Core.BPM.DefinitionBuilder;

public class ConditionalNodeBuilder<TProcess>(
    INode firstNode,
    BProcess process,
    ConditionalNode nodeToConfigure,
    List<INode> ifNodes,
    IProcessNodeModifiableBuilder<TProcess> ifBuilder,
    INodeEvaluatorFactory nodeEvaluatorFactory)
    : ProcessNodeBuilder<TProcess>(firstNode, process, nodeEvaluatorFactory), IConditionalModifiableBuilder<TProcess> where TProcess : Aggregate
{
    public void SetIfNode(List<INode> currentInstances)
    {
        var d = new List<INode>();
        var a = new List<INode>();
        var r = new List<INode>();
        IterateConfiguredProcessRoot(currentInstances, r, d, a);
        nodeToConfigure.IfNodeRoots = r;
    }

    public IProcessNodeModifiableBuilder<TProcess> Else(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure)
    {
        var d = new List<INode>();
        var a = new List<INode>();
        var r = new List<INode>();
        var elseBuilder = new ProcessNodeBuilder<TProcess>(null, process, nodeEvaluatorFactory);
        var configedBranch = (configure.Invoke(elseBuilder));
        var elseNodes = ((BaseNodeDefinition)configedBranch).CurrentBranchInstances;
        IterateConfiguredProcessRoot(elseNodes, r, d, a);

        nodeToConfigure.ElseNodeRoots = r;
        return this;
    }
}