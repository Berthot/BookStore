using System.Text;
using System.Text.Json;
using Application.Abstractions.Idempotency;
using Application.Diagnostics;

namespace WebApi.Filters.Idempotency;

/// <summary>Endpoint filter that enforces idempotency per draft-ietf-httpapi-idempotency-key-header-07.
/// Shared by every POST that requires idempotency semantics; parameterised by store type so each
/// bounded context injects its own schema-scoped store.</summary>
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

        // Buffer the body so both the filter and model binding can read it.
        // EnableBuffering() is called by the Program middleware before routing, so the stream
        // is already a seekable FileBufferingReadStream. Model binding may have advanced the
        // position to the end before this filter runs, so we rewind to 0 before reading here
        // and again after so that subsequent reads (model binding re-entry) also start at 0.
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
            return Results.Content(existing.ResponseBody!, "application/json");
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
            // The handler's commit hit the unique constraint (PostgreSQL 23505):
            // another concurrent request already committed this key.
            // Re-read from the DB (bypasses any stale EF change tracker state).
            var committed = await store.FindAsync(key, context.HttpContext.RequestAborted);
            if (committed is { Status: IdempotencyStatus.Completed })
            {
                BookStoreTelemetry.IdempotencyReplays.Add(1);
                return Results.Content(committed.ResponseBody!, "application/json");
            }
            return Results.Problem(statusCode: 409, title: "Conflict",
                detail: "A request with the same Idempotency-Key is currently being processed.");
        }
        catch (IdempotencyLockTimeoutException)
        {
            // lock_timeout fired (PostgreSQL 55P03): the concurrent request holding this key
            // did not finish within the allowed window.
            return Results.Problem(statusCode: 409, title: "Conflict",
                detail: "A request with the same Idempotency-Key is currently being processed. Retry later.");
        }
    }
}
