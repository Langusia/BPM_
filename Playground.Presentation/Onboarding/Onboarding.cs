using Marten.Events.Aggregation;
using Playground.Presentation;

namespace Playground.Application.Documents.Onboarding;

public enum ClientType
{
    Unknown,
    IsMyCredo,
    NoKYC,
    FullCredo
}

public record CheckedClientType(int ClientId, ClientType ClientType);
public record KYCEnrolled(string KYCParameter);

public class Onboarding
{
    public Guid Id { get; set; }

    public ClientType ClientType { get; set; } = ClientType.Unknown;
    public bool TwoFactorValidated { get; set; }
    public string KYCParameter { get; set; }
    public bool Registered { get; set; }


    private void Apply(Checked2FA @event)
    {
        TwoFactorValidated = @event.IsValid;
    }

    private void Apply(CheckedClientType @event)
    {
        ClientType = @event.ClientType;
    }

    public void Apply(KYCEnrolled @event)
    {
        KYCParameter = @event.KYCParameter;
    }
}