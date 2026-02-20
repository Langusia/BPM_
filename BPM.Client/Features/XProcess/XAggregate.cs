using BPM.Core.Process;
using BPM.Core.Definition;
using BPM.Core.Definition.Interfaces;

namespace BPM.Client.Features.XProcess;

public class XAggregate : Aggregate
{
    public string Name { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public int Score { get; set; }

    public void Apply(S1Completed @event) => Name = @event.Name;
    public void Apply(S2Completed @event) => IsApproved = @event.Approved;
    public void Apply(S3Completed @event) { }
    public void Apply(S4Completed @event) => Score = @event.Score;
    public void Apply(S5Completed @event) { }
    public void Apply(S6Completed @event) { }
    public void Apply(S7Completed @event) { }
}

public class XAggregateDefinition : BpmDefinition<XAggregate>
{
    public override ProcessConfig<XAggregate> DefineProcess(IProcessBuilder<XAggregate> configure) =>
        configure
            .StartWith<S1>()
            .Continue<S2>()
                .Or<S3>()
            .If(x => x.IsApproved, branch =>
                branch.ContinueAnyTime<S4>()
                    .UnlockOptional<S5>())
            .Else(branch =>
                branch.Continue<S6>())
            .Group(g =>
            {
                g.AddStep<S7>();
            })
            .End();
}
