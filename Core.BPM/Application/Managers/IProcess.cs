using MediatR;
using Core.BPM.Interfaces;

namespace Core.BPM.Application.Managers;

/// <summary>
/// Defines the contract for a process in the BPM engine.
/// </summary>
public interface IProcess
{
    Guid Id { get; }
    string AggregateName { get; }


    /// <summary>
    /// Retrieves the aggregate as the specified type.
    /// </summary>
    /// <typeparam name="T">Type of aggregate.</typeparam>
    /// <param name="includeUncommittedEvents">Whether to include uncommitted events in the aggregation.</param>
    /// <returns>The constructed aggregate.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no Apply methods are found for the aggregate type.</exception>
    T AggregateAs<T>(bool includeUncommittedEvents = true) where T : Aggregate;


    /// <summary>
    /// Retrieves the aggregate as the specified type, returning null if none exists.
    /// </summary>
    /// <typeparam name="T">Type of aggregate.</typeparam>
    /// <param name="includeUncommittedEvents">Whether to include uncommitted events in the aggregation.</param>
    /// <returns>The constructed aggregate or null if not found.</returns>
    T? AggregateOrNullAs<T>(bool includeUncommittedEvents = true) where T : Aggregate;

    bool TryAggregateAs<T>(out T? aggregate, bool includeUncommitted = true) where T : Aggregate;

    /// <summary>
    /// Appends new events to the uncommitted event queue.
    /// </summary>
    /// <param name="events">Events to append.</param>
    /// <returns>True if the events were appended successfully, otherwise false.</returns>
    bool AppendEvents(params object[] events);

    /// <summary>
    /// Validates if the given command type can be executed within the process.
    /// </summary>
    /// <typeparam name="T">The command type to validate.</typeparam>
    /// <param name="includeUncommittedEvents">Whether to include uncommitted events in validation.</param>
    /// <returns>True if the command type is valid, otherwise false.</returns>
    bool Validate<T>(bool includeUncommittedEvents = true) where T : IBaseRequest;

    /// <summary>
    /// Retrieves the next available steps in the process.
    /// </summary>
    /// <param name="includeUncommittedEvents">Whether to consider unsaved events in the computation.</param>
    /// <returns>A list of next possible steps, or null if no steps are available.</returns>
    List<INode>? GetNextSteps(bool includeUncommittedEvents = true);
}