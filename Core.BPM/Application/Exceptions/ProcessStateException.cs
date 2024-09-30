namespace Core.BPM.Application.Exceptions;

public class ProcessStateException : Exception
{
    public ProcessStateException(string message) : base(message)
    {
    }
}