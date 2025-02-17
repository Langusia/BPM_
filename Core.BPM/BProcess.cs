using Core.BPM.Interfaces;

namespace Core.BPM;

public class BProcess(Type processType, INode rootNode)
{
    public readonly Type ProcessType = processType;
    public INode RootNode = rootNode;
    private List<INode>? _optionals;

    public BProcessConfig Config { get; set; } = new();
    public List<INode> AllNodes { get; set; } = new();

    public List<List<INode>> AllPossibles { get; set; }
    public List<INode> AllDistinctCommands { get; set; } = [];

    public void AddDistinctCommand(INode node, List<INode>? prevSteps)
    {
        AllDistinctCommands ??= [];
        var cmd = AllDistinctCommands.FirstOrDefault(x => x.CommandType == node.CommandType);
        if (cmd is null)
            AllDistinctCommands.Add(node);
        else
            cmd.PrevSteps?.AddRange(prevSteps.ExceptBy(cmd.PrevSteps.Select(z => z.CommandType), x => x.CommandType).ToList());
    }
}