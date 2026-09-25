using Application.Commons;

namespace Fraud.Api.Infrastructure.Extensions;

public static class OperationResultExtensions
{
    public static IResult ToHttpResult<T>(this OperationResult<T> result, int successStatus = 200)
    {
        if (result.IsSuccess)
            return Results.Json(result.Data, statusCode: successStatus);

        return ToProblemResult(result);
    }

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
