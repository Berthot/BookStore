using Domain.Enums;
using Domain.Rules;
using Domain.Rules.Discrepancy;
using Domain.ValueObjects;
using Tests.Shared.Attributes;
using Tests.Shared.Constants;
using Tests.Shared.Mothers.FraudAnalysis;

namespace Tests.Domain.Rules.Discrepancy;

[Unit]
public sealed class DiscrepancyRulesTests
{
    private static readonly DateTime Now = TestConstants.Dates.FixedUtcNow;
    private static readonly Guid TxId = TestConstants.Ids.TransactionId;

    // --- AmountDeviationRule ---

    [Test]
    public void AmountDeviation_NoHistory_DoesNotHit()
    {
        var context = FraudContextMother.Default() with
        {
            CustomerTransactionCount = AmountDeviationRule.MinHistoryCount - 1,
            CustomerAverageAmount = 100m,
            Amount = new Money(999m, "BRL")
        };

        var result = new AmountDeviationRule().Evaluate(context);

        result.Hit.Should().BeFalse();
        result.RuleCode.Should().Be("AMOUNT_DEVIATION");
    }

    [Test]
    public void AmountDeviation_WithHistoryAndAboveDeviation_Hits()
    {
        var context = FraudContextMother.Default() with
        {
            CustomerTransactionCount = AmountDeviationRule.MinHistoryCount,
            CustomerAverageAmount = 100m,
            Amount = new Money(100m * AmountDeviationRule.DeviationFactor + 1m, "BRL")
        };

        var result = new AmountDeviationRule().Evaluate(context);

        result.Hit.Should().BeTrue();
        result.Weight.Should().Be(AmountDeviationRule.Weight);
    }

    [Test]
    public void AmountDeviation_WithHistoryButBelowDeviation_DoesNotHit()
    {
        var context = FraudContextMother.Default() with
        {
            CustomerTransactionCount = AmountDeviationRule.MinHistoryCount,
            CustomerAverageAmount = 100m,
            Amount = new Money(100m * AmountDeviationRule.DeviationFactor, "BRL")
        };

        var result = new AmountDeviationRule().Evaluate(context);

        result.Hit.Should().BeFalse();
    }

    [Test]
    public void AmountDeviation_AloneLeadsToReview_NeverRejected()
    {
        var context = FraudContextMother.Default() with
        {
            CustomerTransactionCount = AmountDeviationRule.MinHistoryCount,
            CustomerAverageAmount = 50m,
            Amount = new Money(50m * AmountDeviationRule.DeviationFactor + 1m, "BRL")
        };

        var assessment = new FraudRuleSet([new AmountDeviationRule()]).Evaluate(TxId, context, Now);

        assessment.Outcome.Should().Be(Outcome.Review);
    }

    // --- StructuringRule ---

    [Test]
    public void Structuring_AtThreshold_Hits()
    {
        var context = FraudContextMother.Default() with
            { RecentJustBelowThresholdCount = StructuringRule.RecentJustBelowThresholdCount };

        var result = new StructuringRule().Evaluate(context);

        result.Hit.Should().BeTrue();
        result.RuleCode.Should().Be("STRUCTURING");
        result.Weight.Should().Be(StructuringRule.Weight);
    }

    [Test]
    public void Structuring_BelowThreshold_DoesNotHit()
    {
        var context = FraudContextMother.Default() with
            { RecentJustBelowThresholdCount = StructuringRule.RecentJustBelowThresholdCount - 1 };

        var result = new StructuringRule().Evaluate(context);

        result.Hit.Should().BeFalse();
    }

    [Test]
    public void Structuring_AloneLeadsToReview_NeverRejected()
    {
        var context = FraudContextMother.Default() with
            { RecentJustBelowThresholdCount = StructuringRule.RecentJustBelowThresholdCount };

        var assessment = new FraudRuleSet([new StructuringRule()]).Evaluate(TxId, context, Now);

        assessment.Outcome.Should().Be(Outcome.Review);
    }
}
