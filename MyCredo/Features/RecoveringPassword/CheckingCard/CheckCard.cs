using Core.BPM;
using Core.BPM.Application;
using Core.BPM.Attributes;
using Core.BPM.DefinitionBuilder;
using Marten.Events.Projections.Flattened;

namespace MyCredo.Features.RecoveringPassword.CheckingCard;

[BpmProducer(typeof(CheckCardInitiated))]
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

    public void Initiate(long userId, int paymentId, string hash)
    {
        var @event = new CheckCardInitiated(userId, paymentId, hash);
        Enqueue(@event);
        Apply(@event);
    }
}

public class CheckCardDefinition : BpmDefinition<CheckCard>
{
    public override ProcessConfig<CheckCard> DefineProcess(IProcessBuilder<CheckCard> configureProcess)
    {
        return null;
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