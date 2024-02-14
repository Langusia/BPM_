using Marten;

namespace Playground.Application.Documents.Onboarding;

public class OnboardingHandler
{
    private Guid _id;
    private IDocumentStore _store;

    public OnboardingHandler(Guid id, IDocumentStore store)
    {
        _id = id;
        _store = store;
    }


    public void CheckClientType(int clientId, ClientType clType)
    {
        using var session = _store.LightweightSession();
        var str = session.Events.StartStream<Onboarding>(new CheckedClientType(clientId, clType));
        //some ifs and logic
        session.SaveChanges();
    }


    public void EnrollKYC(Guid documentId)
    {
        using var session = _store.LightweightSession();
        var kycParameter = "KYCParameter";
        //some ifs and logic
        session.Events.Append(documentId, new KYCEnrolled(kycParameter));
        session.SaveChanges();
    }


    public void Check2FA(Guid documentId, TwoFactorValidator twoFactorValidator)
    {
        using var session = _store.LightweightSession();
        //some ifs and logic
        var res = twoFactorValidator.ValidateTwoFactor(documentId, "1234", new { }, session);
        session.Events.Append(documentId, new Checked2FA(res));
        session.SaveChanges();
    }

    public Onboarding? Get(Guid id)
    {
        using var session = _store.QuerySession();
        var doc = session.Query<Onboarding>().FirstOrDefault(x => x.Id == id);
        //some ifs and logic
        return doc;
    }

    public Onboarding? AggregateOnboardingStream(Guid id)
    {
        using var session = _store.QuerySession();
        var doc = session.Events.AggregateStream<Onboarding>(id);
        //some ifs and logic
        return doc;
    }
}