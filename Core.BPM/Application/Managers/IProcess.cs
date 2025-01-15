using MediatR;
using Core.BPM.Interfaces;

namespace Core.BPM.Application.Managers;

public interface IProcess
{
    T AggregateAs<T>(bool includeUncommitted = true) where T : Aggregate;
    T AggregateOrDefaultAs<T>(bool includeUncommitted = true) where T : Aggregate;
    T? AggregateOrNullAs<T>(bool includeUncommitted = true) where T : Aggregate;
    bool AppendEvents(params object[] events);
    bool Validate<T>(bool includeUncommitted) where T : IBaseRequest;
    List<INode>? GetNextSteps(bool includeUnsavedEvents = true);
}