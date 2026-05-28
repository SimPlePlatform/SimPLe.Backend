namespace SimPle.Shared.Common;

/// <summary>
/// Represents the result of an operation, carrying either a value or an error.
/// </summary>
public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error? Error { get; }

    private Result(T value) { IsSuccess = true; Value = value; }
    private Result(Error error) { IsSuccess = false; Error = error; }

    public static Result<T> Ok(T value) => new(value);
    public static Result<T> Fail(Error error) => new(error);
    public static Result<T> Fail(string code, string message) => new(new Error(code, message));
}

public sealed class Result
{
    public bool IsSuccess { get; }
    public Error? Error { get; }

    private Result() { IsSuccess = true; }
    private Result(Error error) { IsSuccess = false; Error = error; }

    public static Result Ok() => new();
    public static Result Fail(Error error) => new(error);
    public static Result Fail(string code, string message) => new(new Error(code, message));
}
