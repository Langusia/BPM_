using Core.BPM.Application.Managers;
using Core.BPM.Attributes;
using MediatR;
using MyCredo.Features.Loan.LoanV9;
using MyCredo.Features.RecoveringPassword;

namespace MyCredo.Features.TwoFactor;

[BpmProducer(typeof(OtpSent))]
public record GenerateOtp(Guid ProcessId) : IRequest<long>;

class MyClass
{
    private readonly IBpmStore _store;

    public async void See()
    {
        var process = await _store.FetchProcessAsync(Guid.Empty, CancellationToken.None);
        var s = process.AggregateAs<TwoFactor>(true);
        process.Validate<GenerateOtp>(true);
        process.AppendEvents(new OtpSent(Guid.Empty, "hash"));

        var ss = process.AggregateAs<TwoFactor>(true);
        var sss = process.AggregateAs<IssueLoan>(true);
        var nextNodes = process.GetNextSteps();
        await _store.SaveChangesAsync(CancellationToken.None);


        var passwordRecoveryAgg = process.AggregateAs<PasswordRecovery>(false);
        var twoFactorAgg = process.AggregateAs<TwoFactor>(false);
    }
}

public class GenerateOtpHandler : IRequestHandler<GenerateOtp, long>
{
    private readonly BpmStore<TwoFactor, GenerateOtp> _bpm;

    public GenerateOtpHandler(BpmStore<TwoFactor, GenerateOtp> bpm)
    {
        _bpm = bpm;
    }

    public async Task<long> Handle(GenerateOtp request, CancellationToken cancellationToken)
    {
        var process = await _bpm.AggregateProcessStateAsync(request.ProcessId, cancellationToken);
        if (!process.ValidateOrigin())
            return 0;


        process.AppendEvent(new OtpSent(Guid.NewGuid(), ""));

        //process.Fail("test");

        // if (process.AppendEvent(x => x.Finish(process.Aggregate.Id, "1234")))
        //     return 0;
        //if (!process.AppendEvent(x => x.GenerateOtp(process.Aggregate.Id, "1234")))
        //    return 0;
        await _bpm.SaveChangesAsync(cancellationToken);
        return 9;
    }
}