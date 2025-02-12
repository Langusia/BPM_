using Core.BPM.Nodes;
using Core.BPM.Evaluators;
using Core.BPM.Interfaces;

public class GroupNode(string groupId, Type processType) : NodeBase(typeof(GroupNode), processType), INode
{
    public string GroupId { get; } = groupId;
    public List<INode> SubNodes { get; set; } = [];

    public override INodeStateEvaluator GetEvaluator() => new GroupNodeStateEvaluator();
}