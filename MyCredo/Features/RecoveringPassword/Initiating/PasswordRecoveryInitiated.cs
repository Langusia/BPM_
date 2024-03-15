using Core.BPM.MediatR;
using Core.BPM.MediatR.Mediator;
using MyCredo.Common;
using MyCredo.Features.RecoveringPassword.ValidatingOtp;

namespace MyCredo.Features.RecoveringPassword.Initiating;

public record PasswordRecoveryInitiated(string PersonalNumber, DateTime BirthDate, ChannelTypeEnum ChannelType)
    : CredoEvent<InitiatePasswordRecovery>;