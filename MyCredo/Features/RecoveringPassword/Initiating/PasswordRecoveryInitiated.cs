using Core.BPM.Attributes;
using MyCredo.Common;

namespace MyCredo.Features.RecoveringPassword.Initiating;

public record PasswordRecoveryInitiated(string PersonalNumber, DateTime BirthDate, ChannelTypeEnum ChannelType, bool Initiated) : BpmEvent;