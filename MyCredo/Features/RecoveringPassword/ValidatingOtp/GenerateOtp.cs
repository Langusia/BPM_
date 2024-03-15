using Core.BPM;
using Core.BPM.MediatR;
using Core.BPM.MediatR.Mediator;
using Credo.Core.Shared.Library;
using MediatR;

namespace MyCredo.Features.RecoveringPassword.ValidatingOtp;

[BpmRequest<PasswordRecovery>]
public record GenerateOtp(Guid ProcessId) : IBpmCommand<long>;

public class GenerateOtpHandler : IRequestHandler<GenerateOtp, Result<long>>
{
    private readonly IServiceProvider _serviceProvider;

    public GenerateOtpHandler(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<Result<long>> Handle(GenerateOtp request, CancellationToken cancellationToken)
    {
        return Result.Success<long>(9);
    }
}