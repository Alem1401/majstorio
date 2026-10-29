namespace Majstorio.Model.Results;

public enum ErrorType
{
    Validation,
    Conflict,
    NotFound,
    Unauthorized,
    Forbidden
}

public class Result
{
    public bool IsSuccess { get; }
    public ErrorType? ErrorType { get; }
    public string? Message { get; }

    protected Result(bool isSuccess, ErrorType? errorType = null, string? message = null)
    {
        IsSuccess = isSuccess;
        ErrorType = errorType;
        Message = message;
    }

    public static Result Success() => new(true);

    public static Result Failure(ErrorType errorType, string message)
        => new(false, errorType, message);
}

public sealed class Result<T> : Result
{
    public T? Value { get; }

    private Result(T value) : base(true)
    {
        Value = value;
    }

    private Result(ErrorType errorType, string message) : base(false, errorType, message)
    {
    }

    public static Result<T> Success(T value) => new(value);

    public new static Result<T> Failure(ErrorType errorType, string message)
        => new(errorType, message);
}
