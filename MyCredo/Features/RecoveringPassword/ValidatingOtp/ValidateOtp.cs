using Core.BPM;
using Core.BPM.MediatR;
using Credo.Core.Shared.Library;
using MediatR;

namespace MyCredo.Features.RecoveringPassword.ValidatingOtp;

public record ValidateOtp(Guid DocumentId) : IRequest<Result<long>>;

public record ValidateOtpHandler : IRequestHandler<ValidateOtp, Result<long>>
{
    private readonly BpmProcessManager<PasswordRecovery> _mgr;

    public ValidateOtpHandler(BpmProcessManager<PasswordRecovery> mgr)
    {
        _mgr = mgr;
    }

    public async Task<Result<long>> Handle(ValidateOtp request, CancellationToken cancellationToken)
    {
        await _mgr.ValidateAsync<ValidateOtp>(request.DocumentId, cancellationToken);
        //doStuff
        var isValid = true;
        //
        var s = await _mgr.AppendEvent(request.DocumentId, x => x.ValidateOtp(isValid), cancellationToken);
        return Result.Success(s);
    }
}