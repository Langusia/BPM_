using Core.BPM.Attributes;

namespace MyCredo.Features.RecoveringPassword.GetUserData;

public record UserDataGet(string Username) : BpmEvent;