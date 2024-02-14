using Marten;
using Marten.Events;

namespace Playground.Application.Documents;

public record Checked2FA(bool IsValid):IEvent;

public class TwoFactorValidator
{
    public bool ValidateTwoFactor(Guid documentId, string twofaHandle, object? twofaService,
        IDocumentSession session)
    {
        //validate
        var valid = twofaService is not null;
        session.Events.Append(documentId, new Checked2FA(valid));
        session.SaveChanges();
        return valid;
    }
}
