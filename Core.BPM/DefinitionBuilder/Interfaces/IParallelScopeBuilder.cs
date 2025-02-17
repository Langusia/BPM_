namespace Core.BPM.DefinitionBuilder.Interfaces;

/// <summary>
/// Defines a builder for parallel execution scopes in the BPM process.
/// </summary>
public interface IParallelScopeBuilder<TProcess> where TProcess : Aggregate
{
    /// <summary>
    /// Adds a step to the parallel scope with optional chaining.
    /// </summary>
    /// <typeparam name="TCommand">The command type representing a process step.</typeparam>
    /// <param name="configure">An optional configuration for chaining further steps.</param>
    /// <returns>The same parallel scope builder for further modifications.</returns>
    IParallelScopeBuilder<TProcess> AddStep<TCommand>(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null);
}