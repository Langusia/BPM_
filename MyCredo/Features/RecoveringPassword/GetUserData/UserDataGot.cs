using Core.BPM.BCommand;

namespace MyCredo.Features.RecoveringPassword.GetUserData;

public record UserDataGet(string Username) : BpmEvent;