namespace Core.BPM;

public record BpmResult(bool IsSuccess, Code Code);

public record BpmResult<T>(bool IsSuccess, Code Code, T? Data) : BpmResult(IsSuccess, Code);

public static class Result
{
    public static BpmResult Success() => new BpmResult(true, Code.Success);
    public static BpmResult Fail(Code code) => new BpmResult(false, code);
    public static BpmResult<T?> Success<T>(T? data) => new(true, Code.Success, data);
    public static BpmResult<T?> Fail<T>(Code code, T? data) => new(false, code, data);
}

public enum Code
{
    Success,
    NoSuccess,
    ProcessFailed,
    InvalidEvent,
    Expired
}