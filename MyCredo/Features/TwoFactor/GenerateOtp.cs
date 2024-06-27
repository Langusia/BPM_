using Core.BPM.Application.Managers;
using Core.BPM.Configuration;
using Core.BPM.MediatR.Attributes;
using Core.BPM.MediatR.Managers;
using Marten;
using MediatR;
using MyCredo.Features.RecoveringPassword;

namespace MyCredo.Features.TwoFactor;

[BpmProducer(typeof(GeneratedOtp))]
public record GenerateOtp(Guid ProcessId) : IRequest<long>;

public class GenerateOtpHandler : IRequestHandler<GenerateOtp, long>
{
    private readonly BpmStore<TwoFactor> _bpm;

    public GenerateOtpHandler(BpmStore<TwoFactor> bpm)
    {
        _bpm = bpm;
    }

    public async Task<long> Handle(GenerateOtp request, CancellationToken cancellationToken)
    {
        var s = await _bpm.AggregateProcessStateAsync(request.ProcessId, cancellationToken);
        s.ValidateFor<GenerateOtp>();

        s.AppendEvent(x => x.GenerateOtp(s.Aggregate.Id, "1234"));
        await _bpm.SaveChangesAsync(cancellationToken);
        return 9;
    }
}