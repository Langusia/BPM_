using Core.BPM.BCommand;
using MyCredo.Common;

namespace MyCredo.Features.RecoveringPassword.Initiating;

public record PasswordRecoveryInitiated(string PersonalNumber, DateTime BirthDate, ChannelTypeEnum ChannelType)
    : IBpmEvent;