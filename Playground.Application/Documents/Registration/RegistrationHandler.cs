using Marten;
using Playground.Application.Documents.Onboarding;

namespace Playground.Application.Documents.Registration;

public static class RegistrationHandler
{
    public static Guid InitiateRegistarion(int personalNumber, IDocumentSession session)
    {
        var id = Guid.NewGuid();
        session.Events.StartStream<Registration>(id,
            new CheckedClientType(personalNumber, ClientType.FullCredo)
        );
        //some ifs and logic
        session.SaveChanges();
        return id;
    }

    public static bool Check2Fa(Guid documentId, TwoFactorValidator twoFactorValidator, IDocumentSession session, BpmProcessManager<Reguster, Checked2FA>)
    {
        //some ifs and logic 
        return twoFactorValidator.ValidateTwoFactor(documentId, "1234", new { }, session);
    }
    
    public static bool AddRegistrationParameter(Guid documentId, IDocumentSession session)
    {
        var res = session.Events.AggregateStream<Registration>(documentId);
        //if(!2FAValid)
        //return null;
        //some ifs and logic   
        var regParameter = "KYCPArameter";
        session.Events.Append();
        return twoFactorValidator.ValidateTwoFactor(documentId, "1234", new { }, session);
    }

    public static Registration? AggregateRegistrationStream(Guid id, IDocumentSession session)
    {
        var doc = session.Events.AggregateStream<Registration>(id);
        //some ifs and logic
        return doc;
    }
}