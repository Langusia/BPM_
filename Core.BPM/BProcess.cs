﻿using Core.BPM.Interfaces;

namespace Core.BPM;

public class BProcess(Type processType, INode rootNode)
{
    public readonly Type ProcessType = processType;
    public INode RootNode = rootNode;
    private List<INode>? _optionals;

    public BProcessConfig Config { get; set; } = new();
    public List<INode> AllNodes { get; set; } = new();
}