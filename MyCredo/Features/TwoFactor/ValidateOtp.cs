using Core.BPM.Application.Managers;
using Core.BPM.MediatR.Attributes;
using Core.BPM.MediatR.Managers;
using Credo.Core.Shared.Library;
using MediatR;
using MyCredo.Features.RecoveringPassword.CheckingCard;
using MyCredo.Retail.Loan.Application.Features.TwoFactor.OtpValidate;

namespace MyCredo.Features.TwoFactor;

[BpmProducer(typeof(OtpValidated))]
public record ValidateOtp(Guid DocumentId) : IRequest<Result<long>>;

public record ValidateOtpHandler : IRequestHandler<ValidateOtp, Result<long>>
{
    private readonly BpmStore<TwoFactor> _bpm;

    public ValidateOtpHandler(BpmStore<TwoFactor> bpm)
    {
        _bpm = bpm;
    }

    public async Task<Result<long>> Handle(ValidateOtp request, CancellationToken cancellationToken)
    {
        var s = await _bpm.AggregateProcessStateAsync(request.DocumentId, cancellationToken);
        if (!s.ValidateFor<ValidateOtp>())
            return null;

        s.AppendEvent(x => x.ValidateOtp(true));
        if (!s.ValidateFor<CheckCardInitiate>())
            return null;

        await _bpm.SaveChangesAsync(cancellationToken);
        return 9;
    }
}