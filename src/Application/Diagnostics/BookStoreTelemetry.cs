using System.Diagnostics.Metrics;

namespace Application.Diagnostics;

/// <summary>Centralised observability primitives for the BookStore bounded context (sales and idempotency).</summary>
public static class BookStoreTelemetry
{
    public const string MeterName = "BookStore";

    private static readonly Meter Meter = new(MeterName);

    /// <summary>Number of purchases created or transitioned, tagged by status (PENDING, CONFIRMED, CANCELLED, UNDER_REVIEW).</summary>
    public static readonly Counter<long> Purchases =
        Meter.CreateCounter<long>("bookstore.purchases");

    /// <summary>Number of idempotency key replays (identical request replayed by the client).</summary>
    public static readonly Counter<long> IdempotencyReplays =
        Meter.CreateCounter<long>("bookstore.idempotency.replays");
}
