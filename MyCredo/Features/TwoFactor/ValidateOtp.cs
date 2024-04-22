using Core.BPM.MediatR.Attributes;
using Core.BPM.MediatR.Managers;
using Credo.Core.Shared.Library;
using Marten;
using MediatR;

namespace MyCredo.Features.TwoFactor;

[BpmProducer(typeof(OtpSubmited))]
public record ValidateOtp(Guid DocumentId) : IRequest<Result<long>>;

public record ValidateOtpHandler : IRequestHandler<ValidateOtp, Result<long>>
{
    private readonly BpmManager _bpm;
    private readonly IDocumentSession _session;

    public ValidateOtpHandler(BpmManager mgr1, IDocumentSession session)
    {
        _bpm = mgr1;
        _session = session;
    }

    public async Task<Result<long>> Handle(ValidateOtp request, CancellationToken cancellationToken)
    {
        var s = await _bpm.ValidateAsync<ValidateOtp>(request.DocumentId, cancellationToken);
        //if (s)
        //    return null;
        //
        //await _bpm.ValidateAsync<GenerateOtp>(request.DocumentId, cancellationToken);
        //doStuff
        //await _session.Events.LoadAsync<OtpValidationAggregate>();
        var isValid = true;
        //
        await _bpm.AppendAsync(request.DocumentId, [new OtpSubmited("", true)], cancellationToken);

        return null;
    }
}