using Core.BPM.Configuration;

namespace Core.BPM.Interfaces;

public interface INode<TProcess, TCommand> : INode<TProcess>
    where TProcess : IAggregate
{
    INode<TProcess, TCommand> Or<TNextCommand>(Action<INode<TProcess, TNextCommand>>? configure = null,
        Predicate<TProcess>? expr = null);

    INode<TProcess, TNextCommand> ThenAppendRight<TNextCommand>(Predicate<TProcess>? expr = null);

    INode<TProcess, TNextCommand> ContinueWith<TNextCommand>(
        Action<INode<TProcess, TNextCommand>>? configure = null, Predicate<TProcess>? expr = null);

    void SetConfig(NodeConfig<TProcess> cfg);

    INode<TProcess, TEvent>? MoveTo<TEvent>();
}

public interface INode<TProcess> : INode
    where TProcess : IAggregate
{
    List<INode<TProcess>> GetLastNodes();
    Predicate<TProcess>? Condition { get; set; }
    void GetValidNodes(TProcess aggregate, List<INode<TProcess>> result);
    bool Validate(TProcess aggregate);

    List<INode<TProcess>> NextSteps { get; set; }
    void AddNextStep(INode<TProcess> node);
    List<INode<TProcess>> PrevSteps { get; set; }
    void AddPrevStep(INode<TProcess> node);


    INode<TProcess>? MoveTo(Type eventType);
    Tuple<int, bool> ValidCommandTry(IList<Type> events);
}

public interface INode
{
    Type CommandType { get; }
}

public interface IBNode
{
    int Index { get; }
    Type CommandType { get; }
    Type ProcessType { get; }


    void GetLastNodes(List<IBNode> lastNodes)
    {
        if (NextSteps is not null)
            foreach (var nextStep in NextSteps)
            {
                if (nextStep.NextSteps is null || nextStep.NextSteps.Count == 0)
                    lastNodes.Add(nextStep);
                else
                    GetLastNodes(lastNodes);
            }
    }

    List<IBNode> NextSteps { get; set; }
    void AddNextStep(IBNode node);
    void AddNextSteps(IList<IBNode> nodes);
    List<IBNode> PrevSteps { get; set; }
    void AddPrevStep(IBNode node);
    void AddPrevSteps(IList<IBNode> nodes);
}

public class Node(Type commandType, Type processType) : IBNode
{
    public int Index { get; set; }
    public Type CommandType { get; } = commandType;
    public Type ProcessType { get; } = processType;

    public List<IBNode> NextSteps { get; set; } = new List<IBNode>();

    public void AddNextStep(IBNode node)
    {
        NextSteps ??= new List<IBNode>();
        NextSteps.Add(node);
    }

    public void AddNextSteps(IList<IBNode> nodes)
    {
        NextSteps ??= new List<IBNode>();
        NextSteps.AddRange(nodes);
    }


    public List<IBNode> PrevSteps { get; set; }

    public void AddPrevStep(IBNode node)
    {
        PrevSteps ??= new List<IBNode>();
        PrevSteps.Add(node);
    }

    public void AddPrevSteps(IList<IBNode>? nodes)
    {
        if (nodes is not null)
        {
            PrevSteps ??= new List<IBNode>();
            PrevSteps.AddRange(nodes);
        }
    }
}

public interface IBProcessBuilder<TProcess>
{
    public IBNodeBuilder StartWith<TCommand>()
    {
        var inst = new Node(typeof(TCommand), typeof(TProcess));
        var processInst = new BProcess(typeof(TProcess), inst);
        BProcessGraphConfiguration.AddProcess(processInst);
        return new BConditionNodeBuilder(inst, processInst);
    }
}

public class BProcess(Type processType, IBNode rootNode)
{
    public readonly Type ProcessType = processType;
    public readonly IBNode RootNode = rootNode;
}

public static class BNodeBuilderExtensions
{
    public static IBNodeBuilder ThenContinue(this IBNodeBuilder builder)
    {
        return null;
    }

    public static IBNodeBuilder Or<TNextCommand>(this IBNodeBuilder builder, Action<ICBNodeBuilder>? configure = null)
    {
        return builder.Or<TNextCommand>(configure);
    }
}

public class BConditionNodeBuilder(IBNode rootNode, BProcess process) : IBNodeBuilder
{
    private readonly IBNode _rootNode = rootNode;
    private IBNode _currentNode = rootNode;
    private List<IBNode> _currentNexts = rootNode.NextSteps;
    public List<IBNode> _nodesToAppend = [];

    public BProcess GetProcess()
    {
        return process;
    }

    public void AppendAndMoveNext()
    {
        if (_nodesToAppend.Count != 0)
        {
            _currentNexts = _nodesToAppend;
            _nodesToAppend.RemoveAll(_ => true);
        }
    }

    public IBNodeBuilder Continue<TCommand>(Action<IBNodeBuilder>? configure = null)
    {
        var node = new Node(typeof(TCommand), process.ProcessType);

        if (_currentNode.PrevSteps is not null && _currentNode.PrevSteps?.Count != 0)
            _currentNode.PrevSteps!.ForEach(x => x.NextSteps.ForEach(x => x.AddNextStep(node)));
        else if (_currentNode.NextSteps.Count == 0)
            _currentNode.AddNextStep(node);


        node.AddPrevStep(_currentNode);
        _currentNode = node;

//if (_nodesToAppend.Count != 0)
//{
//    _currentNexts.ForEach(x => x.AddNextSteps(_nodesToAppend));
//    _nodesToAppend.RemoveAll(_ => true);
//}
//else
//{
//    _currentNexts.Add(node);
//}

        return this;
    }

    public IBNodeBuilder Or<TNextCommand>(Action<ICBNodeBuilder>? configure = null)
    {
        var newNode = new Node(typeof(TNextCommand), process.ProcessType);
        _currentNode.PrevSteps.ForEach(x => x.AddNextStep(newNode));
        newNode.AddPrevStep(_currentNode);
        //_nodesToAppend.Add(newNode);

        return this;
    }
}

//public class BNodeBuilder(IBNode currentNode, Type processType) : IBNodeBuilder
//{
//    public IConditionBNodeBuilder AddNext<TCommand>(Action<IBNodeBuilder>? configure = null)
//    {
//        var newNode = new Node(typeof(TCommand), processType);
//        currentNode.AddNextStep(newNode);
//
//        return new BConditionNodeBuilder(currentNode, processType);
//    }
//}

public class BProcessBuilder<TProcess> : IBProcessBuilder<TProcess>
{
    public IBNodeBuilder StartWith<TCommand>()
    {
        var nodeInst = new Node(typeof(TCommand), typeof(TProcess));
        var processInst = new BProcess(typeof(TProcess), nodeInst);

        return new BConditionNodeBuilder(nodeInst, processInst);
    }
}

public static class ConfigExtensions
{
    public static void Build(this IBNodeBuilder builder)
    {
        BProcessGraphConfiguration.AddProcess(builder.GetProcess());
    }
}

public interface ICBNodeBuilder
{
    IBNodeBuilder ThenAdd<Command>(Action<ICBNodeBuilder>? configure = null);
}

public interface IBNodeBuilder // : IBNodeBuilder
{
    BProcess GetProcess();
    IBNodeBuilder Continue<Command>(Action<IBNodeBuilder>? configure = null);
    IBNodeBuilder Or<TNextCommand>(Action<ICBNodeBuilder>? configure = null);
}