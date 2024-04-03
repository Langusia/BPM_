using Core.BPM.Interfaces;

namespace Core.BPM;

public class BProcess(Type processType, INode rootNode)
{
    public readonly Type ProcessType = processType;
    public readonly INode RootNode = rootNode;
}