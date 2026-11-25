using BPM.Core.Attributes;
using BPM.Core.Events;
using MyCredo.Common;

namespace MyCredo.Features.RecoveringPassword.Initiating;

public record PasswordRecoveryInitiated(string PersonalNumber, DateTime BirthDate, ChannelTypeEnum ChannelType, bool Initiated) : BpmEvent;