using System;
using MediatR;

namespace Core.BPM.DefinitionBuilder.Interfaces;

/// <summary>
/// Provides methods for building the initial structure of a process workflow.
/// </summary>
/// <typeparam name="TProcess">The type of process being configured.</typeparam>
public interface IProcessNodeInitialBuilder<TProcess> : IProcessNodeBuilder<TProcess> where TProcess : Aggregate
{
    /// <summary>
    /// Creates a group of nodes that can be executed in parallel(ignoring completion order) within the process.
    /// </summary>
    /// <param name="configure">Action to configure the nodes within the group.</param>
    /// <returns>A builder for modifying the process after the group.</returns>
    IProcessNodeModifiableBuilder<TProcess> Group(Action<IGroupBuilder<TProcess>> configure);

    /// <summary>
    /// Creates a node that transitions to a different process's workflow.
    /// </summary>
    /// <typeparam name="TGuestAggregate">The type of process to transition to.</typeparam>
    /// <param name="sealedSteps">If true, prevents the guest process steps from appearing in nextSteps after completion.</param>
    /// <returns>A builder for modifying the process after the guest process.</returns>
    /// <remarks>
    /// The guest process node is considered completed when either:
    /// 1. The guest process overrides Aggregate method - IsCompleted() and returns true
    /// 2. All nodes in the guest process have been executed and persisted (default behavior)
    /// 
    /// When completed:
    /// - If sealedSteps is true: The guest process steps will no longer appear in nextSteps
    /// - If sealedSteps is false: The guest process steps will remain available in nextSteps
    /// 
    /// Note: Each process is represented by its own aggregate type that manages its state and events.
    /// </remarks>
    IProcessNodeModifiableBuilder<TProcess> JumpTo<TGuestAggregate>(bool sealedSteps = true) where TGuestAggregate : Aggregate;

    /// <summary>
    /// Creates a conditional branch based on the current process state.
    /// </summary>
    /// <param name="predicate">Condition to evaluate against the current process.</param>
    /// <param name="configure">Action to configure the nodes within the conditional branch.</param>
    /// <returns>A builder for modifying the conditional branch.</returns>
    IConditionalModifiableBuilder<TProcess> If(Predicate<TProcess> predicate,
        Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure);

    /// <summary>
    /// Creates a conditional branch based on a different process's state.
    /// </summary>
    /// <typeparam name="T">The type of process to evaluate the condition against.</typeparam>
    /// <param name="predicate">Condition to evaluate against the specified process.</param>
    /// <param name="configure">Action to configure the nodes within the conditional branch.</param>
    /// <returns>A builder for modifying the conditional branch.</returns>
    IConditionalModifiableBuilder<TProcess> If<T>(Predicate<T> predicate,
        Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure) where T : Aggregate;

    /// <summary>
    /// Creates a conditional branch based on the current process state, allowing non-modifiable builders (e.g. UnlockOptional) as the branch result.
    /// </summary>
    IConditionalModifiableBuilder<TProcess> If(Predicate<TProcess> predicate,
        Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeNonModifiableBuilder<TProcess>> configure);

    /// <summary>
    /// Creates a conditional branch based on a different process's state, allowing non-modifiable builders (e.g. UnlockOptional) as the branch result.
    /// </summary>
    IConditionalModifiableBuilder<TProcess> If<T>(Predicate<T> predicate,
        Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeNonModifiableBuilder<TProcess>> configure) where T : Aggregate;

    /// <summary>
    /// Adds a command node that must be executed in sequence within the process.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to execute.</typeparam>
    /// <param name="configure">Optional action to configure additional nodes after the command.</param>
    /// <returns>A builder for modifying the process after the command.</returns>
    IProcessNodeModifiableBuilder<TProcess> Continue<TCommand>(
        Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null)
        where TCommand : IBaseRequest;

    /// <summary>
    /// Adds a command node that can be executed at any time during the process execution.
    /// </summary>
    /// <typeparam name="TCommand">The type of command to execute.</typeparam>
    /// <param name="configure">Optional action to configure additional nodes after the command.</param>
    /// <returns>A builder for modifying the process after the command.</returns>
    IProcessNodeModifiableBuilder<TProcess> ContinueAnyTime<TCommand>(
        Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>>? configure = null)
        where TCommand : IBaseRequest;

    /// <summary>
    /// Creates an optional command node that can be unlocked under certain conditions within the process.
    /// </summary>
    /// <typeparam name="TCommand">The type of command that can be unlocked.</typeparam>
    /// <returns>A builder that prevents further modifications to this branch.</returns>
    IProcessNodeNonModifiableBuilder<TProcess> UnlockOptional<TCommand>() where TCommand : IBaseRequest;
}