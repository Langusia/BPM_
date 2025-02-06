namespace Core.BPM;

public class BpmResult(bool isSuccess, Code code)
{
    public bool IsSuccess { get; set; } = isSuccess;
    public Code Code { get; set; } = code;
}

public class BpmResult<T>(bool isSuccess, Code code, T? data) : BpmResult(isSuccess, code)
{
    public T? Data { get; } = data;
}

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