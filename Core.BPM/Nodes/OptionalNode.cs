using System;
using Core.BPM.Nodes.Evaluation;

namespace Core.BPM.Nodes;

public class OptionalNode(Type commandType, Type processType, INodeEvaluatorFactory nodeEvaluatorFactory) : NodeBase(commandType, processType, nodeEvaluatorFactory);
