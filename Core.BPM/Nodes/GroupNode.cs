using Core.BPM.Nodes;
using Core.BPM.Evaluators;
using Core.BPM.Interfaces;

public class GroupNode(Type processType) : NodeBase(typeof(GroupNode), processType)
{
    public List<INode> SubRootNodes { get; set; } = [];
    private List<INode> _allMemberNodes = [];

    public void SetAllMembers(List<INode> allmembers)
    {
        _allMemberNodes = allmembers;
    }

    public override INodeStateEvaluator GetEvaluator() => new GroupNodeStateEvaluator(this);
    public override bool ContainsEvent(object @event) => _allMemberNodes.Any(x => x.ContainsEvent(@event));
}