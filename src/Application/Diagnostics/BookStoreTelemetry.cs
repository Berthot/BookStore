using System.Diagnostics.Metrics;

namespace Application.Diagnostics;

/// <summary>Centralised observability primitives for the BookStore bounded context (sales and idempotency).</summary>
public static class BookStoreTelemetry
{
    public const string MeterName = "BookStore";

    private static readonly Meter Meter = new(MeterName);

    /// <summary>Purchase state transitions, tagged by status (Confirmed, Cancelled, UnderReview, PendingFraudCheck).</summary>
    public static readonly Counter<long> Purchases =
        Meter.CreateCounter<long>("bookstore.purchases");

    /// <summary>Idempotency requests tagged by result (new|replay).</summary>
    public static readonly Counter<long> IdempotencyRequests =
        Meter.CreateCounter<long>("bookstore.idempotency.requests");

    /// <summary>Stale purchases re-published by the reconciliation job.</summary>
    public static readonly Counter<long> ReconciliationRepublished =
        Meter.CreateCounter<long>("bookstore.reconciliation.republished");

    // --- Gauge (updated by BookStoreMetricsJob every 30 s) ---

    private static long _outboxPending;

    /// <summary>Undelivered messages in the bookstore outbox.</summary>
    public static readonly ObservableGauge<long> OutboxPending =
        Meter.CreateObservableGauge("messaging.outbox.pending",
            () => new Measurement<long>(Volatile.Read(ref _outboxPending),
                new KeyValuePair<string, object?>("context", "bookstore")),
            "messages", "Undelivered messages in the outbox.");

    public static void SetOutboxPending(long value) => Volatile.Write(ref _outboxPending, value);
}
