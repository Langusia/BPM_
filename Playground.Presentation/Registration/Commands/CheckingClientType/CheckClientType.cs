using Core.BPM.Configuration;
using Core.Persistence;
using Credo.Core.Shared.Library;
using Credo.Core.Shared.Mediator;
using MediatR;

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
        var res = BpmConfigurationExtensions.Validate<Registration>(request.GetType());
        if (res)
            return null;

        //LOGIC

        var s = await _repo.GetAndUpdate(request.DocumentId,
            x => x.CheckClientType(request.PersonalNumber, ClientType.Unknown),
            ct: cancellationToken);

        return request.DocumentId;
    }
}