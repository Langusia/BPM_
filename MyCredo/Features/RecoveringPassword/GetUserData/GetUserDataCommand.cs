using BPM.Core.Attributes;
using BPM.Core.Events;
using MediatR;

namespace MyCredo.Features.RecoveringPassword.GetUserData;

[BpmProducer(typeof(UserDataGet))]
public class GetUserDataCommand : IRequest<bool>
{
}