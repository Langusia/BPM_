using System;

namespace BPM.Core.Exceptions;

public class ProcessStateException : Exception
{
    public ProcessStateException(string message) : base(message)
    {
    }
}