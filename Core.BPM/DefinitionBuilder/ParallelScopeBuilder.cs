using Core.BPM.Interfaces;
using Core.BPM.Nodes;

namespace Core.BPM.DefinitionBuilder;

/// <summary>
/// Defines a builder for parallel execution scopes in the BPM process.
/// </summary>
public class ParallelScopeBuilder<TProcess> : IParallelScopeBuilder<TProcess> where TProcess : Aggregate
{
    private readonly List<INode> _parallelNodes = new();
    private readonly IProcessNodeModifiableBuilder<TProcess> _parentBuilder;

    public ParallelScopeBuilder(IProcessNodeModifiableBuilder<TProcess> parentBuilder)
    {
        _parentBuilder = parentBuilder;
    }

    /// <inheritdoc/>
    public IParallelScopeBuilder<TProcess> AddStep<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null)
    {
        var node = new Node(typeof(TCommand), typeof(TProcess));
        _parallelNodes.Add(node);

        if (configure is not null)
        {
            var nextBuilder = new ProcessNodeBuilder<TProcess>(node, new BProcess(typeof(TProcess), node));
            configure(nextBuilder);
        }

        return this;
    }

    /// <inheritdoc/>
    public IProcessNodeModifiableBuilder<TProcess> EndParallelScope()
    {
        // Ensure all parallel nodes are added to the process
        foreach (var node in _parallelNodes)
        {
            // Logic to integrate parallel nodes into the workflow
            // This would depend on how nodes are stored in the BPM engine
        }

        return _parentBuilder;
    }
}