using Core.BPM.MediatR;
using Core.BPM.MediatR.Mediator;

namespace MyCredo.Features.RecoveringPassword.ValidatingOtp;

public record GeneratedOtp : CredoEvent<GenerateOtp>;