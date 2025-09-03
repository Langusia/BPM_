using Core.BPM.Application.Managers;
using Core.BPM.Attributes;
using Credo.Core.Shared.Library;
using MediatR;
using MyCredo.Features.Loan.OtpValidate;
using MyCredo.Features.Loann;
using MyCredo.Features.RecoveringPassword;
using MyCredo.Features.RecoveringPassword.CheckingCard;

namespace MyCredo.Features.TwoFactor;

[BpmProducer(typeof(OtpValidated))]
public record ValidateOtp(Guid DocumentId) : IRequest<Result<long>>;

/// A
public record Ad(Guid DocumentId) : BpmEvent;

[BpmProducer(typeof(Ad))]
public record A(Guid DocumentId) : IRequest<Result<int>>;

public class AHandler(IBpmStore store) : IRequestHandler<A, Result<int>>
{
    public async Task<Result<int>> Handle(A request, CancellationToken cancellationToken)
    {
        store.StartProcess<PasswordRecovery>();
        await store.SaveChangesAsync(cancellationToken);

        return Result.Success(123);
    }
}

/// B
public record Bd(Guid DocumentId) : BpmEvent;

[BpmProducer(typeof(Bd))]
public record B(Guid DocumentId) : IRequest<Result<Guid>>;

public class BHandler(IBpmStore store) : IRequestHandler<B, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(B request, CancellationToken cancellationToken)
    {
        var s = store.StartProcess<RequestDigitalLoan>(new Bd(request.DocumentId));
        await store.SaveChangesAsync(cancellationToken);

        return Result.Success(s.Id);
    }
}

/// Query Q
public record Q(Guid DocumentId) : IRequest<Result<Guid>>;

public class QHandler(IBpmStore store) : IRequestHandler<Q, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(Q request, CancellationToken cancellationToken)
    {
        var p = await store.FetchProcessAsync(request.DocumentId, cancellationToken);
        var s = p.AggregateAs<RequestDigitalLoan>();

        return Result.Success(p.Id);
    }
}

/// Query Q2
public record Q2(Guid DocumentId) : IRequest<Result<Guid>>;

public class Q2Handler(IBpmStore store) : IRequestHandler<Q2, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(Q2 request, CancellationToken cancellationToken)
    {
        var p = await store.FetchProcessAsync(request.DocumentId, cancellationToken);
        var s = p.AggregateAs<PasswordRecovery>();

        return Result.Success(s.Id);
    }
}

/// A
[BpmProducer(typeof(Cd))]
public record C(Guid DocumentId) : IRequest<Result<long>>;

public record Cd(Guid DocumentId, bool IsCd) : BpmEvent;

/// A
[BpmProducer(typeof(Dd))]
public record D(Guid DocumentId) : IRequest<Result<long>>;

public record Dd(Guid DocumentId) : BpmEvent;

/// A
[BpmProducer(typeof(Ed))]
public record E(Guid DocumentId) : IRequest<Result<long>>;

public record Ed(Guid DocumentId) : BpmEvent;

/// A
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
    public ValidateOtpHandler()
    {
    }

    public async Task<Result<long>> Handle(ValidateOtp request, CancellationToken cancellationToken)
    {
        //BL
        return 9;
    }
}