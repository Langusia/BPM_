// using Core.BPM.Interfaces;
//
// namespace Core.BPM;
//
// public class BpmProcess<TProcess>(IBBNode<TProcess> rootNode)
//     : BpmProcess(typeof(TProcess), rootNode), IProcess<TProcess> where TProcess : IAggregate
// {
//     public IBNode<TProcess> RootNode { get; } = rootNode;
//     protected override IBBNode<TProcess> GetRootNode() => rootNode;
//
//     public IBNode<TProcess>? MoveTo<TCommand>() => rootNode.MoveTo(typeof(TCommand));
//
//     public List<IBNode<TProcess>> GetConditionValidGraphNodes(TProcess aggregate)
//     {
//         var result = new List<IBBNode<TProcess>>();
//         rootNode.GetValidNodes(aggregate, result);
//         return result;
//     }
// }
//
// public class BpmProcess(Type processType, INode rootNode)
// {
//     public readonly Type ProcessType = processType;
//
//     protected virtual IBNode GetRootNode() => rootNode;
// }
//
