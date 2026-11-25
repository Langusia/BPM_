using BPM.Core.Attributes;
using BPM.Core.Events;

namespace MyCredo.Features.RecoveringPassword.GetUserData;

public record UserDataGet(string Username) : BpmEvent;