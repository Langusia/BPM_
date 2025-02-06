using Core.BPM.Application.Managers;
using Core.BPM.Attributes;
using JasperFx.CodeGeneration.Model;
using MediatR;
using MyCredo.Features.Loan.LoanV9;
using MyCredo.Features.RecoveringPassword;

namespace MyCredo.Features.TwoFactor;

[BpmProducer(typeof(OtpSent))]
public record GenerateOtp(Guid ProcessId) : IRequest<long>;

public class GenerateOtpHandler : IRequestHandler<GenerateOtp, long>
{
    private readonly IBpmStore _bpm;

    public GenerateOtpHandler(IBpmStore bpm)
    {
        _bpm = bpm;
    }

    public async Task<long> Handle(GenerateOtp request, CancellationToken cancellationToken)
    {
        var process = await _bpm.FetchProcessAsync(request.ProcessId, cancellationToken);
        var v = process.Validate<GenerateOtp>();
        var vc = process.Validate<GenerateContract>();

        process.AppendFail<GenerateOtp>("asd", request);
        var agg3 = process.AggregateOrNullAs<TwoFactor>();
        var agg1 = process.AggregateAs<PasswordRecovery>();
        var sss = process.AppendEvents(new OtpSent(Guid.NewGuid(), "test"));
        var s = new PasswordRecovery();
        if (!process.TryAggregateAs<PasswordRecovery>(out var aggregate))
        {
            return 0;
        }


        await _bpm.SaveChangesAsync(cancellationToken);
        return 9;
    }
}