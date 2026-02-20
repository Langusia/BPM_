using System;

namespace Core.BPM.Exceptions;

public class BpmWrongProcessPathException : Exception
{
    private BpmWrongProcessPathException(string processName, string commandName, string id, string continuations) :
        base(
            @$"Command: '{commandName}' not expected. Available continuations for '{processName}'
                 with id:'{id}' are: {continuations}")
    {
    }


    public static BpmWrongProcessPathException For<TP, TC>(Guid id, string[]? expectedNodes)
    {
        string conts = "None";
        if (expectedNodes is not null)
            conts = string.Join(",", expectedNodes);

        return new BpmWrongProcessPathException(typeof(TP).Name, typeof(TC).Name, id.ToString(), conts);
    }
}