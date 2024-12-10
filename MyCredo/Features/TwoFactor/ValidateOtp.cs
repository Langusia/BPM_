using Core.BPM.Application.Managers;
using Core.BPM.Attributes;
using Credo.Core.Shared.Library;
using MediatR;
using MyCredo.Features.Loan.OtpValidate;
using MyCredo.Features.RecoveringPassword.CheckingCard;

namespace MyCredo.Features.TwoFactor;

[BpmProducer(typeof(OtpValidated))]
public record ValidateOtp(Guid DocumentId) : IRequest<Result<long>>;

[BpmProducer(typeof(OtpValidated))]
public record A(Guid DocumentId) : IRequest<Result<long>>;

[BpmProducer(typeof(OtpValidated))]
public record B(Guid DocumentId) : IRequest<Result<long>>;

[BpmProducer(typeof(OtpValidated))]
public record C(Guid DocumentId) : IRequest<Result<long>>;

[BpmProducer(typeof(OtpValidated))]
public record D(Guid DocumentId) : IRequest<Result<long>>;

[BpmProducer(typeof(OtpValidated))]
public record E(Guid DocumentId) : IRequest<Result<long>>;

[BpmProducer(typeof(OtpValidated))]
public record F(Guid DocumentId) : IRequest<Result<long>>;

[BpmProducer(typeof(OtpValidated))]
public record G(Guid DocumentId) : IRequest<Result<long>>;

[BpmProducer(typeof(OtpValidated))]
public record H(Guid DocumentId) : IRequest<Result<long>>;

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

        processState.AppendEvent(new OtpValidated(Guid.NewGuid(), true));

        await _bpm.SaveChangesAsync(cancellationToken);
        return 9;
    }
}