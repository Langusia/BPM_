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
    private readonly BpmManager<TwoFactor> _bpm;

    public GenerateOtpHandler(BpmManager<TwoFactor> bpm)
    {
        _bpm = bpm;
    }

    public async Task<long> Handle(GenerateOtp request, CancellationToken cancellationToken)
    {
        var s = await _bpm.AggregateAsync<GenerateOtp>(request.ProcessId, cancellationToken);
        ////doStuff
        var @event = new GeneratedOtp(request.ProcessId, "1234");
        await _bpm.AppendAsync(request.ProcessId, [@event], cancellationToken);
        return 9;
    }
}