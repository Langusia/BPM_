using Core.BPM;
using Core.BPM.Interfaces.Builder;
using Core.BPM.MediatR;
using Marten.Events.Projections.Flattened;

namespace MyCredo.Features.RecoveringPassword.CheckingCard;

public class CheckCard : Aggregate
{
    public long UserId { get; set; }
    public int PaymentId { get; set; }
    public string Hash { get; set; }

    public void Apply(CheckCardInitiated @event)
    {
        UserId = @event.UserId;
        PaymentId = @event.PaymentId;
        Hash = @event.Hash;
    }
}

public class CheckCardDefinition : BpmDefinition<CheckCard>
{
    public override void DefineProcess(IProcessBuilder<CheckCard> configureProcess)
    {
        // configureProcess.StartWith<InitiatedCardCheck>()
        //     .Continue<FinishCheckCard>();
    }
}

public class CheckCardFlatProjection : FlatTableProjection
{
    public CheckCardFlatProjection() : base("check_card", SchemaNameSource.EventSchema)
    {
        Table.AddColumn<Guid>("id").AsPrimaryKey();
        Project<CheckCardInitiated>(map =>
        {
            map.Map(x => x.PaymentId);
            map.Map(x => x.Hash);
            map.Map(x => x.UserId);
        });
    }
}