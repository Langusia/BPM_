using Core.BPM;
using Core.BPM.Interfaces.Builder;
using Core.BPM.MediatR;
using Marten;
using Marten.Events;
using Marten.Events.Aggregation;
using Marten.Events.Projections;
using Marten.Events.Projections.Flattened;
using Weasel.Postgresql.Tables;

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
        //configureProcess.StartWith<InitiatedCardCheck>()
        //    .Continue<FinishCheckCard>();
    }
}

public class CheckCardProjection : SingleStreamProjection<CheckCard>
{
    public CheckCard Create(CheckCardInitiated init)
    {
        return new CheckCard
        {
            UserId = init.UserId,
            PaymentId = init.PaymentId,
            Hash = init.Hash
        };
    }
}

public class CheckCardEventProjection : EventProjection
{
    public CheckCardEventProjection()
    {
        var table = new Table("check_card");
        table.AddColumn<Guid>("id").AsPrimaryKey();
        table.AddColumn<string>("payment_id").NotNull();
        SchemaObjects.Add(table);
    }


    public void Project(IEvent<CheckCardInitiated> e, IDocumentOperations ops)
    {
        ops.QueueSqlCommand("insert into check_card (id, payment_id) values (?, ?)",
            e.StreamId, e.Data.PaymentId
        );
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