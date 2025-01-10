using Core.BPM.Application.Managers;
using Core.BPM.Attributes;
using Core.BPM.BCommand;
using Credo.Core.Shared.Library;
using MediatR;
using MyCredo.Features.Loan.OtpValidate;
using MyCredo.Features.RecoveringPassword.CheckingCard;

namespace MyCredo.Features.TwoFactor;

[BpmProducer(typeof(OtpValidated))]
public record ValidateOtp(Guid DocumentId) : IRequest<Result<long>>;

[BpmProducer(typeof(Ad))]
public record A(Guid DocumentId) : IRequest<Result<long>>;

public record Ad(Guid DocumentId) : BpmEvent;

[BpmProducer(typeof(Bd))]
public record B(Guid DocumentId) : IRequest<Result<long>>;

public record Bd(Guid DocumentId) : BpmEvent;

[BpmProducer(typeof(Cd))]
public record C(Guid DocumentId) : IRequest<Result<long>>;

public record Cd(Guid DocumentId) : BpmEvent;

[BpmProducer(typeof(Dd))]
public record D(Guid DocumentId) : IRequest<Result<long>>;

public record Dd(Guid DocumentId) : BpmEvent;

[BpmProducer(typeof(Ed))]
public record E(Guid DocumentId) : IRequest<Result<long>>;

public record Ed(Guid DocumentId) : BpmEvent;

[BpmProducer(typeof(Fd))]
public record F(Guid DocumentId) : IRequest<Result<long>>;

public record Fd(Guid DocumentId) : BpmEvent;

[BpmProducer(typeof(Gd))]
public record G(Guid DocumentId) : IRequest<Result<long>>;

public record Gd(Guid DocumentId) : BpmEvent;

[BpmProducer(typeof(Hd))]
public record H(Guid DocumentId) : IRequest<Result<long>>;

public record Hd(Guid DocumentId) : BpmEvent;

[BpmProducer(typeof(Zd))]
public record Z(Guid DocumentId) : IRequest<Result<long>>;

public record Zd(Guid DocumentId) : BpmEvent;

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