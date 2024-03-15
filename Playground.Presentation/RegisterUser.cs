using Core.BPM.Configuration;
using Marten;
using MediatR;
using Playground.Application.Documents;

namespace Playground.Presentation;

public record RegisterUser(Guid UserId, string Name) : IRequest<bool>;

public record ChangeMobile
{
    public bool FaceRecognized { get; set; }
    public bool OtpValidated { get; set; }
    public bool MobildeChanged { get; set; }
}

public class RegisterUserHandler : IRequestHandler<RegisterUser, bool>
{
    private IDocumentStore _store;

    public RegisterUserHandler(IDocumentStore store)
    {
        _store = store;
    }

    public async Task<bool> Handle(RegisterUser request, CancellationToken cancellationToken)
    {
        var cfg = BpmProcessGraphConfiguration.GetConfig<Registration.Registration>();


        await using var session = _store.LightweightSession();
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirstName = request.Name,
            LastName = request.Name,
            Internal = true
        };
        session.Store(user);
        await session.SaveChangesAsync();
        return true;
    }
}