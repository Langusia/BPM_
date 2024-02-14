using Marten;
using MediatR;
using Playground.Application.Documents;

namespace Playground.Application;

public record GetUsers : IRequest<List<User>>;

public class GetUsersHandler : IRequestHandler<GetUsers, List<User>>
{
    private IDocumentStore _store;

    public GetUsersHandler(IDocumentStore store)
    {
        _store = store;
    }

    public async Task<List<User>> Handle(GetUsers request, CancellationToken cancellationToken)
    {
        await using var session = _store.QuerySession();
        var res = await session.Query<User>().ToListAsync(cancellationToken);

        return res.ToList();
    }
}