using Application.Commons;

namespace WebApi.Extensions;

public static class OperationResultExtensions
{
    /// <summary>Maps an OperationResult to an IResult (Problem Details on failure). Success status must be provided explicitly.</summary>
    public static IResult ToHttpResult<T>(this OperationResult<T> result, int successStatus = 200)
    {
        if (result.IsSuccess)
            return Results.Json(result.Data, statusCode: successStatus);

        return ToProblemResult(result);
    }

    /// <summary>Maps a non-generic OperationResult to an IResult (Problem Details on failure).</summary>
    public static IResult ToHttpResult(this OperationResult result, int successStatus = 200)
    {
        if (result.IsSuccess)
            return Results.StatusCode(successStatus);

        return ToProblemResult(result);
    }

    private static IResult ToProblemResult(OperationResult result) =>
        result.ErrorCode switch
        {
            ErrorCode.Validation => Results.ValidationProblem(
                result.Errors.Select((e, i) => ($"[{i}]", new[] { e }))
                    .ToDictionary(x => x.Item1, x => x.Item2)),
            ErrorCode.NotFound => Results.Problem(statusCode: 404, title: "Not Found",
                detail: result.Errors.FirstOrDefault()),
            ErrorCode.Conflict => Results.Problem(statusCode: 409, title: "Conflict",
                detail: result.Errors.FirstOrDefault()),
            ErrorCode.Unprocessable => Results.Problem(statusCode: 422, title: "Unprocessable Entity",
                detail: result.Errors.FirstOrDefault()),
            ErrorCode.Unauthorized => Results.Problem(statusCode: 401, title: "Unauthorized"),
            ErrorCode.Forbidden => Results.Problem(statusCode: 403, title: "Forbidden"),
            _ => Results.Problem(statusCode: 500, title: "Internal Server Error")
        };
}
