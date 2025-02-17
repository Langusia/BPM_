using Core.BPM.Attributes;

namespace MyCredo.Features.RecoveringPassword.CheckingCard;

public record CheckCardInitiated(long UserId, int PaymentId, string Hash) : BpmEvent;