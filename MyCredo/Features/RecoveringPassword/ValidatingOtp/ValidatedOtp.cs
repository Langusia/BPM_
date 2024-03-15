using Core.BPM.MediatR;
using Core.BPM.MediatR.Mediator;

namespace MyCredo.Features.RecoveringPassword.ValidatingOtp;

public record ValidatedOtp(bool IsValid) : CredoEvent<ValidateOtp>;