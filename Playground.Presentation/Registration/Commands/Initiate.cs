using Core.BPM.MediatR;
using Core.Persistence;
using MediatR;

namespace Playground.Presentation.Registration.Commands;

public record Initiate(string PersonalNumber) : IRequest<Guid>;

public class InitiateHandler : IRequestHandler<Initiate, Guid>
{
    private readonly MartenRepository<Registration> _repo;
    public InitiateHandler(MartenRepository<Registration> repo)
    {
        _repo = repo;
    }


    public async Task<Guid> Handle(Initiate request, CancellationToken cancellationToken)
    {
        
        var docId = Guid.NewGuid();
        var s = await _repo.Add(Registration.Initiate(docId), ct: cancellationToken);
        return docId;
    }
}