using Core.BPM.AggregateConditions;
using Core.BPM.Interfaces;

namespace Core.BPM.DefinitionBuilder;

public class ConditionalNodeBuilder<TProcess>(INode rootNode, BProcess process, List<IAggregateCondition>? aggregateConditions = null) : ProcessNodeBuilder<TProcess>(rootNode, process, aggregateConditions), IConditionalModifiableBuilder<TProcess> where TProcess : Aggregate
{
    public IProcessNodeModifiableBuilder<TProcess> Else(Func<IProcessNodeInitialBuilder<TProcess>, IProcessNodeModifiableBuilder<TProcess>> configure)
    {
        throw new NotImplementedException();
    }
}