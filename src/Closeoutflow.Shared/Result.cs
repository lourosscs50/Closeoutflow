namespace Closeoutflow.Shared;

public class Result
{
    protected Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new ArgumentException("Successful result cannot have an error.", nameof(error));

        }
        if (!isSuccess && error == Error.None)
        {
            throw new ArgumentException("Failed result must have an error.", nameof(error));
        }
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
}

public class Result<T> : Result
{
    private readonly T? _value;

    protected Result(T value) : base(true, Error.None)
    {
        _value = value;
    }

    protected internal Result(Error error) : base(false, error)
    {
        _value = default;
    }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access the value of a failed result.");

    public static Result<T> Success(T value) => new(value);
    public new static Result<T> Failure(Error error) => new(error);
}