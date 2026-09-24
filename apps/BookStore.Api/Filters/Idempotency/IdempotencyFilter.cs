using System.Text;
using System.Text.Json;
using Application.Abstractions.Idempotency;
using Application.Diagnostics;

namespace BookStore.Api.Filters.Idempotency;

public sealed class IdempotencyFilter<TStore>(TStore store) : IEndpointFilter
    where TStore : IIdempotencyStore
{
    public const string HttpContextEntryKey = "IdempotencyEntry";

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var key = context.HttpContext.Request.Headers["Idempotency-Key"].ToString();
        if (string.IsNullOrWhiteSpace(key))
            return Results.Problem(statusCode: 400, title: "Bad Request",
                detail: "Idempotency-Key header is required.");

        context.HttpContext.Request.EnableBuffering();
        context.HttpContext.Request.Body.Position = 0;
        string body;
        using (var reader = new StreamReader(
            context.HttpContext.Request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            leaveOpen: true))
        {
            body = await reader.ReadToEndAsync(context.HttpContext.RequestAborted);
        }
        context.HttpContext.Request.Body.Position = 0;

        string hash;
        try
        {
            hash = IdempotencyHasher.ComputeHash(body);
        }
        catch (JsonException)
        {
            return Results.Problem(statusCode: 400, title: "Bad Request",
                detail: "Request body must be valid JSON.");
        }

        var existing = await store.FindAsync(key, context.HttpContext.RequestAborted);

        if (existing is { Status: IdempotencyStatus.Processing })
            return Results.Problem(statusCode: 409, title: "Conflict",
                detail: "A request with the same Idempotency-Key is currently being processed.");

        if (existing is { Status: IdempotencyStatus.Completed })
        {
            if (existing.BodyHash != hash)
                return Results.Problem(statusCode: 422, title: "Unprocessable Entity",
                    detail: "A request with the same Idempotency-Key was submitted with a different body.");

            BookStoreTelemetry.IdempotencyReplays.Add(1);
            return ReplayResult(context.HttpContext, existing);
        }

        var entry = IdempotencyEntry.Create(key, hash, DateTime.UtcNow);
        store.Add(entry);
        context.HttpContext.Items[HttpContextEntryKey] = entry;

        try
        {
            return await next(context);
        }
        catch (IdempotencyConflictException)
        {
            var committed = await store.FindAsync(key, context.HttpContext.RequestAborted);
            if (committed is { Status: IdempotencyStatus.Completed })
            {
                BookStoreTelemetry.IdempotencyReplays.Add(1);
                return ReplayResult(context.HttpContext, committed);
            }
            return Results.Problem(statusCode: 409, title: "Conflict",
                detail: "A request with the same Idempotency-Key is currently being processed.");
        }
        catch (IdempotencyLockTimeoutException)
        {
            return Results.Problem(statusCode: 409, title: "Conflict",
                detail: "A request with the same Idempotency-Key is currently being processed. Retry later.");
        }
    }

    private static IResult ReplayResult(HttpContext httpContext, IdempotencyEntry entry)
    {
        httpContext.Response.Headers["Idempotent-Replayed"] = "true";
        if (!string.IsNullOrEmpty(entry.LocationHeader))
            httpContext.Response.Headers.Location = entry.LocationHeader;
        var statusCode = entry.StatusCode > 0 ? entry.StatusCode : 200;
        return Results.Content(entry.ResponseBody!, "application/json", statusCode: statusCode);
    }
}
