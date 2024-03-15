using Core.BPM.Interfaces;
using Marten;

namespace Playground.Presentation;

public record Checked2FA(Guid DocumentId, bool IsValid) : IBpmEvent;

public class TwoFactorValidator
{
    public bool ValidateTwoFactor(Guid documentId, string twofaHandle, object? twofaService,
        IDocumentSession session)
    {
        //validate
        var valid = twofaService is not null;
        session.Events.Append(documentId, new Checked2FA(documentId, valid));
        session.SaveChanges();
        return valid;
    }
}