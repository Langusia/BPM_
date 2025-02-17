using Core.BPM.Evaluators.Factory;

namespace Core.BPM.Nodes;

public class OptionalNode(Type commandType, Type processType, INodeEvaluatorFactory nodeEvaluatorFactory) : NodeBase(commandType, processType, nodeEvaluatorFactory);