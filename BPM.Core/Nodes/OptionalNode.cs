using System;
using BPM.Core.Nodes.Evaluation;

namespace BPM.Core.Nodes;

public class OptionalNode(Type commandType, Type processType, INodeEvaluatorFactory nodeEvaluatorFactory) : NodeBase(commandType, processType, nodeEvaluatorFactory);
