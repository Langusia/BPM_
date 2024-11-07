using Core.BPM.Application.Managers;
using Core.BPM.Attributes;
using MediatR;

namespace MyCredo.Features.TwoFactor;

[BpmProducer(typeof(OtpSent))]
public record GenerateOtp(Guid ProcessId) : IRequest<long>;

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

        // if (process.AppendEvent(x => x.Finish(process.Aggregate.Id, "1234")))
        //     return 0;
        //if (!process.AppendEvent(x => x.GenerateOtp(process.Aggregate.Id, "1234")))
        //    return 0;
        await _bpm.SaveChangesAsync(cancellationToken);
        return 9;
    }
}