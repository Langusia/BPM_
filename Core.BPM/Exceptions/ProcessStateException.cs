using System;

namespace Core.BPM.Exceptions;

public class ProcessStateException : Exception
{
    public ProcessStateException(string message) : base(message)
    {
    }
}