using System;
using System.Collections.Generic;
using BPM.Core.Nodes;

namespace BPM.Core.Process;

public class BProcess(Type processType, INode rootNode)
{
    public readonly Type ProcessType = processType;
    public INode RootNode = rootNode;
    private List<INode>? _optionals;

    public BProcessConfig Config { get; set; } = new();
    public List<INode> AllNodes { get; set; } = new();
}