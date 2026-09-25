using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Application.Diagnostics;

/// <summary>Centralised observability primitives for the fraud analysis bounded context. All instruments use the same Meter so a single AddMeter call covers everything.</summary>
public static class FraudTelemetry
{
    public const string MeterName = "FraudAnalysis";
    public const string ActivitySourceName = "FraudAnalysis";

    private static readonly Meter Meter = new(MeterName);

    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    /// <summary>Number of transactions submitted to the fraud engine.</summary>
    public static readonly Counter<long> TransactionsReceived =
        Meter.CreateCounter<long>("fraud.transactions.received");

    /// <summary>Number of fraud decisions issued, tagged by outcome (Approved|Rejected|Review) and decider (ENGINE|SYSTEM|REVIEWER).</summary>
    public static readonly Counter<long> Decisions =
        Meter.CreateCounter<long>("fraud.decisions");

    /// <summary>Number of fraud rule triggers, tagged by rule_code.</summary>
    public static readonly Counter<long> RuleHits =
        Meter.CreateCounter<long>("fraud.rule.hits");

    /// <summary>Duration in seconds from transaction received to decision recorded.</summary>
    public static readonly Histogram<double> DecisionDuration =
        Meter.CreateHistogram<double>("fraud.decision.duration", "s",
            "Time in seconds from transaction creation to fraud decision.");

    /// <summary>Idempotency requests tagged by result (new|replay).</summary>
    public static readonly Counter<long> IdempotencyRequests =
        Meter.CreateCounter<long>("fraud.idempotency.requests");

    // --- Gauge (updated by FraudMetricsJob every 30 s) ---

    private static long _outboxPending;

    /// <summary>Undelivered messages in the fraud outbox.</summary>
    public static readonly ObservableGauge<long> OutboxPending =
        Meter.CreateObservableGauge("messaging.outbox.pending_messages",
            () => new Measurement<long>(Volatile.Read(ref _outboxPending),
                new KeyValuePair<string, object?>("context", "fraud")),
            "messages", "Undelivered messages in the outbox.");

    public static void SetOutboxPending(long value) => Volatile.Write(ref _outboxPending, value);
}
