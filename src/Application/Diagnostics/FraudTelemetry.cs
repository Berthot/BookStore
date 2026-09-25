using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Application.Diagnostics;

public static class FraudTelemetry
{
    public const string MeterName = "FraudAnalysis";
    public const string ActivitySourceName = "FraudAnalysis";

    private static readonly Meter Meter = new(MeterName);

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    public static readonly Counter<long> TransactionsReceived =
        Meter.CreateCounter<long>("fraud.transactions.received");

    /// <summary>Tagged by outcome (APPROVED|REJECTED|REVIEW) and decider (ENGINE|SYSTEM|REVIEWER).</summary>
    public static readonly Counter<long> Decisions =
        Meter.CreateCounter<long>("fraud.decisions");

    /// <summary>Tagged by rule_code.</summary>
    public static readonly Counter<long> RuleHits =
        Meter.CreateCounter<long>("fraud.rule.hits");

    public static readonly Histogram<double> DecisionDuration =
        Meter.CreateHistogram<double>("fraud.decision.duration", "s",
            "Time in seconds from transaction creation to fraud decision.");

    /// <summary>Tagged by result (new|replay).</summary>
    public static readonly Counter<long> IdempotencyRequests =
        Meter.CreateCounter<long>("fraud.idempotency.requests");

    // --- Gauge (updated by FraudMetricsJob every 30 s) ---

    private static long _outboxPending;

    public static readonly ObservableGauge<long> OutboxPending =
        Meter.CreateObservableGauge("messaging.outbox.pending_messages",
            () => new Measurement<long>(Volatile.Read(ref _outboxPending),
                new KeyValuePair<string, object?>("context", "fraud")),
            "messages", "Undelivered messages in the outbox.");

    public static void SetOutboxPending(long value) => Volatile.Write(ref _outboxPending, value);
}
