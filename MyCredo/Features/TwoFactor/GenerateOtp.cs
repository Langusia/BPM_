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
    private readonly BpmManager _bpm;
    private readonly IDocumentSession _ds;
    private readonly IQuerySession _qs;

    public GenerateOtpHandler(BpmManager bpm, IDocumentSession ds, IQuerySession qs)
    {
        _ds = ds;
        _qs = qs;
    }

    public async Task<long> Handle(GenerateOtp request, CancellationToken cancellationToken)
    {
        //var ss = await _bpm.AggregateAsync<PasswordRecovery>(request.ProcessId, cancellationToken);
        await _bpm.ValidateAsync<GenerateOtp>(request.ProcessId, cancellationToken);
        ////doStuff
        /// _qs

        //await _qs.Events.AggregateStreamAsync<PasswordRecovery>();
        //_ds.Events.Append()

        
        var ss = await _qs.Events.AggregateStreamAsync<PasswordRecovery>(request.ProcessId);
        var strId = Guid.NewGuid();
        var s = new GeneratedOtp(request.ProcessId, "1234");

        _ds.Events.Append(request.ProcessId, s);
        await _ds.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        //_bpm.Append(ot);
        return 9;
    }
}