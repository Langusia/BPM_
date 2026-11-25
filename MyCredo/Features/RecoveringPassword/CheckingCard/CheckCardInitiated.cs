using BPM.Core.Attributes;
using BPM.Core.Events;

namespace MyCredo.Features.RecoveringPassword.CheckingCard;

public record CheckCardInitiated(long UserId, int PaymentId, string Hash) : BpmEvent;