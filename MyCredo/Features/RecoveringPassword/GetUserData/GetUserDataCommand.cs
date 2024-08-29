using Core.BPM.Attributes;
using MediatR;

namespace MyCredo.Features.RecoveringPassword.GetUserData;

[BpmProducer(typeof(UserDataGet))]
public class GetUserDataCommand : IRequest<bool>
{
}