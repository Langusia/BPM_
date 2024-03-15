using Core.BPM.Configuration;
using Core.Persistence;
using Credo.Core.Shared.Library;
using Credo.Core.Shared.Mediator;

namespace Playground.Presentation.Registration.Commands.CheckingClientType;

public record CheckClientType(Guid DocumentId, string PersonalNumber) : ICommand<Guid?>;

public class CheckClientTypeHandler : ICommandHandler<CheckClientType, Guid?>
{
    private MartenRepository<Registration> _repo;

    public CheckClientTypeHandler(MartenRepository<Registration> repo)
    {
        _repo = repo;
    }

    public async Task<Result<Guid?>> Handle(CheckClientType request, CancellationToken cancellationToken)
    {
        var agg = await _repo.Get(request.DocumentId, cancellationToken: cancellationToken);
        var res = BpmProcessGraphConfiguration.GetConfig<Registration>();
        var validNodes = res.GetConditionValidGraphNodes(agg);
        if (validNodes.Any(x => x.CommandType == typeof(CheckClientType)))
            return null;

        //LOGIC

        var s = await _repo.GetAndUpdate(request.DocumentId,
            x => x.CheckClientType(request.PersonalNumber, ClientType.Unknown),
            ct: cancellationToken);

        return request.DocumentId;
    }
}