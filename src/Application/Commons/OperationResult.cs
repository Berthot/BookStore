namespace Application.Commons;

public class OperationResult
{
    public bool IsSuccess { get; init; }
    public ErrorCode ErrorCode { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];

    public static OperationResult Success() =>
        new() { IsSuccess = true, ErrorCode = ErrorCode.None };

    public static OperationResult Fail(ErrorCode code, string error) =>
        new() { IsSuccess = false, ErrorCode = code, Errors = [error] };

    public static OperationResult Fail(ErrorCode code, IEnumerable<string> errors) =>
        new() { IsSuccess = false, ErrorCode = code, Errors = errors.ToArray() };
}

public sealed class OperationResult<T> : OperationResult
{
    public T? Data { get; init; }

    public static OperationResult<T> SuccessResult(T data) =>
        new() { IsSuccess = true, ErrorCode = ErrorCode.None, Data = data };

    public new static OperationResult<T> Fail(ErrorCode code, string error) =>
        new() { IsSuccess = false, ErrorCode = code, Errors = [error] };

    public new static OperationResult<T> Fail(ErrorCode code, IEnumerable<string> errors) =>
        new() { IsSuccess = false, ErrorCode = code, Errors = errors.ToArray() };
}
