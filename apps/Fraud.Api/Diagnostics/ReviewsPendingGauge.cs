using System.Diagnostics.Metrics;
using Application.Diagnostics;

namespace Fraud.Api.Diagnostics;

/// <summary>Owns the fraud.reviews.pending gauge so it is only registered in Fraud.Api, not in Fraud.Worker.</summary>
internal static class ReviewsPendingGauge
{
    private static readonly Meter Meter = new(FraudTelemetry.MeterName);
    private static long _value;

    static ReviewsPendingGauge()
    {
        Meter.CreateObservableGauge(
            "fraud.reviews.pending",
            () => Volatile.Read(ref _value),
            "reviews",
            "Transactions currently awaiting human review.");
    }

    public static void Set(long value) => Volatile.Write(ref _value, value);
}
