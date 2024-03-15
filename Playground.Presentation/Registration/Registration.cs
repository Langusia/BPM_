using Core.BPM;
using Core.BPM.MediatR;
using Marten.Events.Aggregation;
using Playground.Presentation.Registration.Commands;
using Playground.Presentation.Registration.Commands.CheckingClientType;
using Playground.Presentation.Registration.Commands.EnrollingKYC;

namespace Playground.Presentation.Registration;

public class KycParameters
{
    public bool KYCParam1 { get; set; }
    public string KYCParam2 { get; set; }
    public int KYCParam3 { get; set; }
}

public class Registration : Aggregate
{
    public static Registration Initiate(Guid id)
    {
        return new Registration
        {
            Id = id
        };
    }

    public ClientType? ClientType { get; set; }

    public KycParameters? KycParameters { get; set; }

    public void EnrollKYC(string PersonId)
    {
        if (ClientType is null)
            return;

        var @event = new EnrolledKYC(PersonId, true, "", 0);
        Enqueue(@event);
        Apply(@event);
    }

    private void Apply(EnrolledKYC @event)
    {
        if (KycParameters is null)
            KycParameters = new KycParameters();

        KycParameters.KYCParam1 = @event.KYCParam1;
        KycParameters.KYCParam2 = @event.KYCParam2;
        KycParameters.KYCParam3 = @event.KYCParam3;
    }


    public void CheckClientType(string personId, ClientType clientType)
    {
        //logic
        var @event = new CheckedClientType(personId, clientType);
        Enqueue(@event);
        Apply(@event);
    }

    private void Apply(CheckedClientType @event)
    {
        ClientType = @event.ClientType;
    }
}

public class RegistrationProjection : SingleStreamProjection<Registration>
{
    public void Apply(CheckedClientType @event, Registration snapshot)
    {
        snapshot.ClientType = @event.ClientType;
    }

    public void Apply(EnrolledKYC @event, Registration snapshot)
    {
        if (snapshot.KycParameters is null)
            snapshot.KycParameters = new KycParameters();

        snapshot.KycParameters.KYCParam1 = @event.KYCParam1;
        snapshot.KycParameters.KYCParam2 = @event.KYCParam2;
        snapshot.KycParameters.KYCParam3 = @event.KYCParam3;
    }
}

public class RegistrationDefinition : BpmProcessGraphDefinition<Registration>
{
    public override void Define(BpmProcessGraphConfigurator<Registration> configurator)
    {
        configurator.StartWith<Playground.Presentation.Registration.Commands.CheckingClientType.CheckClientType>()
            .ContinueWith<EnrollKYC>(x =>
                x.ContinueWith<CheckClientType>())
            .Or<Initiate>(x =>
                x.ContinueWith<EnrollKYC>());
    }
}