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

    /// <summary>Number of fraud decisions issued, tagged by outcome (APPROVED, REJECTED, REVIEW).</summary>
    public static readonly Counter<long> Decisions =
        Meter.CreateCounter<long>("fraud.decisions");

    /// <summary>Number of fraud rule triggers, tagged by rule_code.</summary>
    public static readonly Counter<long> RuleHits =
        Meter.CreateCounter<long>("fraud.rule.hits");

    /// <summary>Duration in seconds from transaction received to decision recorded. Seconds follow OTel semantic conventions.</summary>
    public static readonly Histogram<double> DecisionDuration =
        Meter.CreateHistogram<double>("fraud.decision.duration", "s",
            "Time in seconds from transaction creation to fraud decision.");
}
