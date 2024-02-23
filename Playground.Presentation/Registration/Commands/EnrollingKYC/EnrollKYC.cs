using Core.Persistence;
using Credo.Core.Shared.Library;
using Credo.Core.Shared.Mediator;
using MediatR;

namespace Playground.Presentation.Registration.Commands.EnrollingKYC;

public record EnrollKYC(Guid DocumentId, string PersonalNumber) : ICommand<Guid>;

public class EnrollKYCHandler : ICommandHandler<EnrollKYC, Guid>
{
    private MartenRepository<Registration> _repo;

    public EnrollKYCHandler(MartenRepository<Registration> repo)
    {
        _repo = repo;
    }


    public async Task<Result<Guid>> Handle(EnrollKYC request, CancellationToken cancellationToken)
    {
        var s = await _repo.GetAndUpdate(request.DocumentId,
            x => x.EnrollKYC(request.PersonalNumber),
            ct: cancellationToken);

        return request.DocumentId;
    }
}