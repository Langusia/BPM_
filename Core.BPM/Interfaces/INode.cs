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


    List<IBNode> NextSteps { get; set; }
    void AddNextStep(IBNode node);
    void AddNextStepToTail(IBNode node);
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

    private void GetLastNodes(List<IBNode> lastNodes)
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

    public void AddNextStepToTail(IBNode node)
    {
        if (NextSteps.Count == 0)
            NextSteps.Add(node);
        else
        {
            var tails = new List<IBNode>();
            GetLastNodes(tails);
            tails.ForEach(x => x.AddNextStep(node));
        }
    }

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

public static class RootExtensions
{
    public static IExtendableNodeBuilder Or1<TCommand>(this IExtendableNodeBuilder builder, Action<IBNodeBuilder>? configure = null)
    {
        var p = builder.GetProcess();
        var r = builder.GetRoot();
        var n = new Node(typeof(TCommand), p.ProcessType);
        //add prev to new
        n.AddPrevStep(r);
        //add next to root
        if (r.PrevSteps is not null)
            r.PrevSteps.ForEach(x => x.NextSteps.ForEach(z => z.AddNextStep(n)));
        else
            r.AddNextStep(n);
        configure?.Invoke(builder);
        return builder;
    }
}

public class BProcess(Type processType, IBNode rootNode)
{
    public readonly Type ProcessType = processType;
    public readonly IBNode RootNode = rootNode;
}

public class BConditionNodeBuilder : IBNodeBuilder, IExtendableNodeBuilder
{
    private IBNode _rootNode;
    private IBNode _currentNode;
    private List<IBNode> _currentNexts;
    public List<IBNode> _nodesToAppend = [];

    private readonly BConditionNodeBuilder _rootContext;
    private readonly BProcess _process;

    public BConditionNodeBuilder(IBNode rootNode, BProcess process, BConditionNodeBuilder rootContext)
    {
        _rootContext = rootContext;
        _process = process;
        _rootNode = rootNode;
        _currentNode = rootNode;
        _currentNexts = rootNode.NextSteps;
    }

    public IBNode GetCurrentRoot()
    {
        return _rootContext.GetCurrent();
    }

    public BConditionNodeBuilder(IBNode rootNode, BProcess process)
    {
        _process = process;
        _rootNode = rootNode;
        _currentNode = rootNode;
        _currentNexts = rootNode.NextSteps;
    }

    public BProcess GetProcess()
    {
        return _process;
    }

    public IBNode GetCurrent()
    {
        return _currentNode;
    }

    public void AddNextToRoot(IBNode node)
    {
        if (_rootContext is not null)
            _rootContext.GetCurrent().AddNextStep(node);
        else
        {
            _rootNode.AddNextStep(node);
        }
    }

    public void AppendAndMoveNext()
    {
        if (_nodesToAppend.Count != 0)
        {
            _currentNexts = _nodesToAppend;
            _nodesToAppend.RemoveAll(_ => true);
        }
    }

    public IExtendableNodeBuilder Continue<TCommand>(Action<IBNodeBuilder>? configure = null)
    {
        var node = new Node(typeof(TCommand), _process.ProcessType);

        //if (_currentNode.PrevSteps is not null && _currentNode.PrevSteps?.Count != 0)
        //    _currentNode.PrevSteps!.ForEach(x => x.NextSteps.ForEach(x => x.AddNextStepToTail(node)));
        if (_rootNode is not null)
            _rootNode.AddNextStepToTail(node);


        node.AddPrevStep(_currentNode);
        if (configure is not null)
        {
            var nextNodeBuilder = new BConditionNodeBuilder(node, _process, this);
            configure?.Invoke(nextNodeBuilder);
            _currentNode.AddNextStep(nextNodeBuilder.GetCurrent());
        }

        _rootNode = _currentNode;
        _currentNode = node;

        return this;
    }

    public IBNode GetRoot() => _rootNode;

    public IBNodeBuilder ContinueRoot<TCommand>(Action<IBNodeBuilder>? configure = null)
    {
        var node = new Node(typeof(TCommand), _process.ProcessType);

        if (_currentNode.PrevSteps is not null && _currentNode.PrevSteps?.Count != 0)
            _currentNode.PrevSteps!.ForEach(x => x.NextSteps.ForEach(x => x.AddNextStep(node)));
        else if (_currentNode.NextSteps.Count == 0)
            _currentNode.AddNextStep(node);


        node.AddPrevStep(_currentNode);
        configure?.Invoke(new BConditionNodeBuilder(_currentNode, _process));

        return this;
    }

    public IBNodeBuilder Or<TNextCommand>(Action<ICBNodeBuilder>? configure = null)
    {
        var newNode = new Node(typeof(TNextCommand), _process.ProcessType);
        _currentNode.PrevSteps.ForEach(x => x.AddNextStep(newNode));
        newNode.AddPrevStep(_currentNode);
        //_nodesToAppend.Add(newNode);

        return this;
    }
}

public class BProcessBuilder<TProcess> : IBProcessBuilder<TProcess>
{
    public IBNodeBuilder StartWith<TCommand>()
    {
        var nodeInst = new Node(typeof(TCommand), typeof(TProcess));
        var processInst = new BProcess(typeof(TProcess), nodeInst);
        BProcessGraphConfiguration.AddProcess(processInst);
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

public interface IExtendableNodeBuilder : IBNodeBuilder
{
    IBNode GetRoot();
}

public interface IBNodeBuilder // : IBNodeBuilder
{
    IBNodeBuilder ContinueRoot<TCommand>(Action<IBNodeBuilder>? configure = null);
    BProcess GetProcess();
    IBNode GetCurrent();

    void AddNextToRoot(IBNode node);

    IExtendableNodeBuilder Continue<Command>(Action<IBNodeBuilder>? configure = null);
    //IBNodeBuilder Or<TNextCommand>(Action<ICBNodeBuilder>? configure = null);
}