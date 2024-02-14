namespace Core.BPM.Interfaces;

public interface IEvent
{
    Guid DocumentId { get; }
}

//public record EEvent : IEvent
//{
//    string[] FollowingNodes { get; }
//}