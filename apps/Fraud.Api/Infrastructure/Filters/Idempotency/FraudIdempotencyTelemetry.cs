using Application.Diagnostics;
using Web.Filters.Idempotency;

namespace Fraud.Api.Infrastructure.Filters.Idempotency;

internal sealed class FraudIdempotencyTelemetry : IIdempotencyTelemetry
{
    public void RecordRequest(string result) =>
        FraudTelemetry.IdempotencyRequests.Add(1,
            new System.Collections.Generic.KeyValuePair<string, object?>("result", result));
}
