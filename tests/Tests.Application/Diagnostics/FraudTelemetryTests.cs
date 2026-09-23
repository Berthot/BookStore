using System.Diagnostics.Metrics;
using Application.Diagnostics;
using Tests.Shared.Attributes;
using Tests.Shared.Base;

namespace Tests.Application.Diagnostics;

[Unit]
public sealed class FraudTelemetryTests : UnitTestsBase
{
    [Test]
    public void All_six_metrics_are_defined_with_correct_names()
    {
        FraudTelemetry.TransactionsReceived.Name.Should().Be("fraud.transactions.received");
        FraudTelemetry.Decisions.Name.Should().Be("fraud.decisions");
        FraudTelemetry.RuleHits.Name.Should().Be("fraud.rule.hits");
        FraudTelemetry.DecisionDuration.Name.Should().Be("fraud.decision.duration");
        BookStoreTelemetry.Purchases.Name.Should().Be("bookstore.purchases");
        BookStoreTelemetry.IdempotencyReplays.Name.Should().Be("idempotency.replays");
    }

    [Test]
    public void ActivitySource_has_correct_name()
    {
        FraudTelemetry.ActivitySource.Name.Should().Be(FraudTelemetry.ActivitySourceName);
    }

    [Test]
    public void RuleHits_increments_once_per_triggered_rule_with_rule_code_tag()
    {
        var ruleCodes = new List<string>();

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Name == "fraud.rule.hits")
                l.EnableMeasurementEvents(instrument, null);
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            foreach (var tag in tags)
                if (tag.Key == "rule_code") ruleCodes.Add(tag.Value?.ToString() ?? "");
        });
        listener.Start();

        FraudTelemetry.RuleHits.Add(1, new KeyValuePair<string, object?>("rule_code", "CARD_VELOCITY"));
        FraudTelemetry.RuleHits.Add(1, new KeyValuePair<string, object?>("rule_code", "HIGH_VALUE_DIGITAL"));

        ruleCodes.Should().HaveCount(2);
        ruleCodes.Should().Contain("CARD_VELOCITY");
        ruleCodes.Should().Contain("HIGH_VALUE_DIGITAL");
    }

    [Test]
    public void Decisions_counter_increments_with_outcome_tag()
    {
        var outcomes = new List<string>();

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (instrument.Name == "fraud.decisions")
                l.EnableMeasurementEvents(instrument, null);
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            foreach (var tag in tags)
                if (tag.Key == "outcome") outcomes.Add(tag.Value?.ToString() ?? "");
        });
        listener.Start();

        FraudTelemetry.Decisions.Add(1, new KeyValuePair<string, object?>("outcome", "APPROVED"));

        outcomes.Should().ContainSingle().Which.Should().Be("APPROVED");
    }
}
