using Core.BPM.AggregateConditions;
using Core.BPM.Exceptions;
using Core.BPM.Interfaces;
using Core.BPM.Nodes;

namespace Core.BPM.DefinitionBuilder;

public class ProcessNodeBuilder<TProcess>(INode rootNode, BProcess process, List<IAggregateCondition>? aggregateConditions = null)
    : BaseNodeDefinition(rootNode, process),
        IProcessNodeModifiableBuilder<TProcess>, IProcessNodeNonModifiableBuilder<TProcess>, IProcessNodeInitialBuilder<TProcess> where TProcess : Aggregate
{
    private ProcessNodeBuilder<TProcess>? _orScopeBuilder;
    private List<IAggregateCondition>? _aggregateConditions = aggregateConditions;

    private ProcessNodeBuilder<TProcess> Continue(ProcessNodeBuilder<TProcess> nextBuilder,
        Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null)
    {
        nextBuilder.CurrentBranchInstances.ForEach(x =>
        {
            x.SetPrevSteps(CurrentBranchInstances);
            x.AggregateConditions = _aggregateConditions;
        });
        if (configure is not null)
        {
            var configuredBuilder = (ProcessNodeBuilder<TProcess>)configure.Invoke(nextBuilder);
            configuredBuilder._orScopeBuilder = this;
            return configuredBuilder;
        }

        return nextBuilder;
    }

    private IProcessNodeModifiableBuilder<TProcess> Or(INode node, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null)
    {
        if (configure is not null)
        {
            var configured = (ProcessNodeBuilder<TProcess>)configure.Invoke(new ProcessNodeBuilder<TProcess>(node, Process));
            if (_orScopeBuilder is null)
            {
                node.SetPrevSteps(GetRoot().PrevSteps);

                process.AddDistinctCommand(node, GetRoot().PrevSteps);
            }
            else
            {
                node.SetPrevSteps(_orScopeBuilder.CurrentBranchInstances);
            }

            node.AggregateConditions = _aggregateConditions;
            CurrentBranchInstances.AddRange(configured.CurrentBranchInstances);
            return this;
        }

        if (_orScopeBuilder is null)
            node.SetPrevSteps(GetRoot().PrevSteps);
        else
        {
            node.SetPrevSteps(_orScopeBuilder.CurrentBranchInstances);
        }

        node.AggregateConditions = _aggregateConditions;

        CurrentBranchInstances.Add(node);
        return this;
    }

    private IProcessNodeModifiableBuilder<TProcess> CaseInternal<T>(Predicate<T> predicate, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure)
        where T : Aggregate
    {
        _aggregateConditions ??= [];
        _aggregateConditions.Add(new AggregateCondition<T>(predicate));
        var configured = (ProcessNodeBuilder<TProcess>)configure.Invoke(this);
        configured._aggregateConditions = null;
        return configured;
    }

    public IProcessNodeModifiableBuilder<TProcess> ParallelScope(Action<IParallelScopeBuilder<TProcess>> configure)
    {
        var parallelBuilder = new ParallelScopeBuilder<TProcess>(this);
        configure(parallelBuilder);
        return parallelBuilder.EndParallelScope();
    }

    public IProcessNodeModifiableBuilder<TProcess> Case<TAggregate>(Predicate<TAggregate> predicate, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure)
        where TAggregate : Aggregate
    {
        return CaseInternal(predicate, configure);
    }

    public IProcessNodeModifiableBuilder<TProcess> Case(Predicate<TProcess> predicate, Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure)
    {
        return CaseInternal(predicate, configure);
    }

    public IProcessNodeModifiableBuilder<TProcess> Or(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure)
    {
        var nextBuilder = new ProcessNodeBuilder<TProcess>(rootNode, process);
        var configured = (ProcessNodeBuilder<TProcess>)configure.Invoke(nextBuilder);

        return configured;
    }


    public IProcessNodeModifiableBuilder<TProcess> Continue<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null)
    {
        return Continue(new ProcessNodeBuilder<TProcess>(new Node(typeof(TCommand), GetProcess().ProcessType), Process, _aggregateConditions), configure);
    }

    public IProcessNodeModifiableBuilder<TProcess> ContinueAnyTime<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null)
    {
        return Continue(new ProcessNodeBuilder<TProcess>(new AnyTimeNode(typeof(TCommand), GetProcess().ProcessType), Process, _aggregateConditions), configure);
    }

    public IProcessNodeNonModifiableBuilder<TProcess> UnlockOptional<TCommand>()
    {
        return Continue(new ProcessNodeBuilder<TProcess>(new OptionalNode(typeof(TCommand), GetProcess().ProcessType), Process, _aggregateConditions));

        var node = new OptionalNode(typeof(TCommand), GetProcess().ProcessType);
        Process.AddOptional(node, CurrentBranchInstances);
        Process.AddDistinctCommand(node, CurrentBranchInstances);
        return this;
    }


    public IProcessNodeModifiableBuilder<TProcess> Or<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null)
    {
        var node = new Node(typeof(TCommand), typeof(TProcess));

        return Or(node, configure);
    }


    public IProcessNodeModifiableBuilder<TProcess> OrOptional<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null)
    {
        var node = new OptionalNode(typeof(TCommand), typeof(TProcess));
        return Or(node, configure);
    }

    public IProcessNodeModifiableBuilder<TProcess> OrAnyTime<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null)
    {
        var node = new OptionalNode(typeof(TCommand), typeof(TProcess));
        return Or(node, configure);
    }


    private List<List<INode>> GetAllPossibles(INode rootNode)
    {
        List<List<INode>> result = [[rootNode]];
        IterateAllPossibles(rootNode, result);
        return result;
    }

    private void IterateAllPossibles(INode root, List<List<INode>> results)
    {
        int ct = 0;
        var init = new List<INode>(results.Last());
        foreach (var t in root.NextSteps)
        {
            if (ct == 0)
                results.Last().Add(t);
            else
            {
                var nextResultSet = new List<INode>(init) { t };
                results.Add(nextResultSet);
            }

            IterateAllPossibles(t, results);
            ct++;
        }
    }

    private Tuple<INode, List<INode>> GetConfiguredProcessRootReverse()
    {
        var currentNodeSet = CurrentBranchInstances;
        List<INode> result = [];
        List<INode> distresult = [];
        IterateConfiguredProcessRoot(currentNodeSet, result, distresult);
        return new Tuple<INode, List<INode>>(result.FirstOrDefault()!, distresult);
    }

    private void IterateConfiguredProcessRoot(List<INode> currentNodeSet, List<INode> res, List<INode> distinctNodeSet)
    {
        foreach (var currentBranchInstance in currentNodeSet)
        {
            if (currentBranchInstance.PrevSteps is null)
            {
                res.Add(currentBranchInstance);
                if (!distinctNodeSet.Exists(x => x.CommandType == currentBranchInstance.CommandType && x.GetType() == currentBranchInstance.GetType()))
                    distinctNodeSet.Add(currentBranchInstance);
                continue;
            }

            foreach (var currentBranchInstancePrevStep in currentBranchInstance.PrevSteps)
            {
                if (!currentBranchInstancePrevStep.NextSteps!.Contains(currentBranchInstance))
                {
                    if (currentBranchInstancePrevStep.NextSteps.Select(x => x.CommandType).Contains(currentBranchInstance.CommandType))
                        throw new SameCommandOnSameLevelDiffBranchFoundException(currentBranchInstance.CommandType.Name);

                    if (!distinctNodeSet.Exists(x => x.CommandType == currentBranchInstance.CommandType && x.GetType() == currentBranchInstance.GetType()))
                        distinctNodeSet.Add(currentBranchInstance);
                    if (distinctNodeSet.Exists(x => x.CommandType == currentBranchInstance.CommandType && x.GetType() != currentBranchInstance.GetType()))
                        throw new SameCommandDiffNodeTypeException(currentBranchInstance.CommandType.Name);

                    currentBranchInstancePrevStep.AddNextStep(currentBranchInstance);
                }
            }

            IterateConfiguredProcessRoot(currentBranchInstance.PrevSteps!, res, distinctNodeSet);
        }
    }

    public MyClass<TProcess> End()
    {
        var res = GetConfiguredProcessRootReverse();
        Process.RootNode = res.Item1;
        Process.AllDistinctCommands = res.Item2;
        var allPossibles = GetAllPossibles(Process.RootNode);
        //var countMore = allPossibles.SelectMany(x => x.GroupBy(x => x.CommandType).Select(c => new
        //{
        //    node = c.First(),
        //    count = c.Count()
        //}).Where(z => z.count > 1)).ToList();
        //
        //if (countMore.Any())
        //    throw new SameCommandForBranchFoundException(countMore.Select(x => x.node.CommandType.Name).Distinct().ToArray());

        Process.AllPossibles = allPossibles;


        return new MyClass<TProcess>();
    }
}