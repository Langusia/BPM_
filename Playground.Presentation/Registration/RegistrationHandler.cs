using Marten;
using Playground.Application.Documents.Onboarding;

namespace Playground.Presentation.Registration;

public static class RegistrationHandler
{
    public static Guid InitiateRegistarion(int personalNumber, IDocumentSession session)
    {
        var id = Guid.NewGuid();
        session.Events.StartStream<Application.Documents.Registration.Registration>(id,
            new CheckedClientType(personalNumber, ClientType.FullCredo)
        );
        //some ifs and logic
        session.SaveChanges();
        return id;
    }

    public static bool Check2Fa(Guid documentId, TwoFactorValidator twoFactorValidator, IDocumentSession session)
    {
        //some ifs and logic 
        return twoFactorValidator.ValidateTwoFactor(documentId, "1234", new { }, session);
    }

    public static bool AddRegistrationParameter(Guid documentId, IDocumentSession session)
    {
        var res = session.Events.AggregateStream<Application.Documents.Registration.Registration>(documentId);
        //if(!2FAValid)
        //return null;
        //some ifs and logic   
        var regParameter = "KYCPArameter";
        session.Events.Append(documentId, new KYCEnrolled(regParameter));
        return true;
    }

    public static Application.Documents.Registration.Registration? AggregateRegistrationStream(Guid id, IDocumentSession session)
    {
        var doc = session.Events.AggregateStream<Application.Documents.Registration.Registration>(id);
        //some ifs and logic
        return doc;
    }
}