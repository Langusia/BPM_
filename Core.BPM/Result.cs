using Core.BPM.Interfaces;
using MediatR;

namespace Core.BPM;

public class AggregateState<T>(T aggregate, BProcess? config) : BpmState
    where T : Aggregate
{
    public T Aggregate { get; init; } = aggregate;

    public bool ValidateFor<TCommand>()
    {
        if (aggregate!.PersistedEvents.Count == 0)
            return config.RootNode == typeof(TCommand);

        var persEventsCopy = new List<string>(aggregate!.PersistedEvents);
        var currentStep = config.RootNode;

        foreach (var step in currentStep.NextSteps!)
        {
            if (step.Validate(persEventsCopy))
                currentStep = step;
        }

        return persEventsCopy.Count == 0 && currentStep.NextSteps!.Any(x => x.CommandType == typeof(TCommand));
    }


}

public class BpmState
{
    public Guid AggregateId { get; init; }
    public INode CurrentNode { get; init; }
    public List<string>? NextNodes { get; init; }
}