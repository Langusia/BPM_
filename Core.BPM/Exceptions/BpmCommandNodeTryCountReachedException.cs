using System;

namespace Core.BPM.Exceptions;

public class BpmCommandNodeTryCoundReachedException : Exception
{
    private BpmCommandNodeTryCoundReachedException(string commandName, string processTypeName, int currentCount) : base(
        $"command '{commandName}' of '{processTypeName}' process reached it's maximum try count. current count is:'{currentCount}'")
    {
    }

    public static BpmCommandNodeTryCoundReachedException For<TP, TC>(int currentCount) =>
        new BpmCommandNodeTryCoundReachedException(typeof(TC).Name, typeof(TP).Name, currentCount);
}