using Application.Diagnostics;
using Web.Filters.Idempotency;

namespace BookStore.Api.Infrastructure.Filters.Idempotency;

internal sealed class BookStoreIdempotencyTelemetry : IIdempotencyTelemetry
{
    public void RecordRequest(string result) =>
        BookStoreTelemetry.IdempotencyRequests.Add(1,
            new System.Collections.Generic.KeyValuePair<string, object?>("result", result));
}
