using Core.BPM.Application.Managers;
using Core.BPM.MediatR.Attributes;
using Credo.Core.Shared.Library;
using MediatR;
using MyCredo.Features.RecoveringPassword.CheckingCard;
using MyCredo.Retail.Loan.Application.Features.TwoFactor.OtpSend;
using MyCredo.Retail.Loan.Application.Features.TwoFactor.OtpValidate;

namespace MyCredo.Features.TwoFactor;

[BpmProducer(typeof(OtpValidated))]
public record ValidateOtp(Guid DocumentId) : IRequest<Result<long>>;

public record ValidateOtpHandler : IRequestHandler<ValidateOtp, Result<long>>
{
    private readonly BpmStore<TwoFactor, ValidateOtp> _bpm;

    public ValidateOtpHandler(BpmStore<TwoFactor, ValidateOtp> bpm)
    {
        _bpm = bpm;
    }

    public async Task<Result<long>> Handle(ValidateOtp request, CancellationToken cancellationToken)
    {
        var processState = await _bpm.AggregateProcessStateAsync(request.DocumentId, cancellationToken);
        if (!processState.ValidateFor<ValidateOtp>())
            return null;

        //BL

        processState.AppendEvent(x => x.ValidateOtp(true));

        await _bpm.SaveChangesAsync(cancellationToken);
        return 9;
    }
}