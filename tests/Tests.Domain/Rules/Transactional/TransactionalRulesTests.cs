using Domain.Enums;
using Domain.Rules;
using Domain.Rules.Transactional;
using Domain.ValueObjects;
using Tests.Shared.Attributes;
using Tests.Shared.Constants;
using Tests.Shared.Mothers.FraudAnalysis;

namespace Tests.Domain.Rules.Transactional;

[Unit]
public sealed class TransactionalRulesTests
{
    private static readonly DateTime Now = TestConstants.Dates.FixedUtcNow;
    private static readonly Guid TxId = TestConstants.Ids.TransactionId;

    // --- HighValueDigitalRule ---

    [Test]
    public void HighValueDigital_DigitalAboveThreshold_Hits()
    {
        var context = FraudContextMother.Default() with
        {
            Amount = new Money(HighValueDigitalRule.AmountThreshold + 1m, "BRL"),
            Delivery = DeliveryType.Digital
        };

        var result = new HighValueDigitalRule().Evaluate(context);

        result.Hit.Should().BeTrue();
        result.RuleCode.Should().Be("HIGH_VALUE_DIGITAL");
        result.Weight.Should().Be(HighValueDigitalRule.Weight);
    }

    [Test]
    public void HighValueDigital_PhysicalAboveThreshold_DoesNotHit()
    {
        var context = FraudContextMother.Default() with
        {
            Amount = new Money(HighValueDigitalRule.AmountThreshold + 1m, "BRL"),
            Delivery = DeliveryType.Physical
        };

        var result = new HighValueDigitalRule().Evaluate(context);

        result.Hit.Should().BeFalse();
        result.Weight.Should().Be(0m);
    }

    [Test]
    public void HighValueDigital_DigitalAtThreshold_DoesNotHit()
    {
        var context = FraudContextMother.Default() with
        {
            Amount = new Money(HighValueDigitalRule.AmountThreshold, "BRL"),
            Delivery = DeliveryType.Digital
        };

        var result = new HighValueDigitalRule().Evaluate(context);

        result.Hit.Should().BeFalse();
    }

    // --- NewCustomerHighAmountRule ---

    [Test]
    public void NewCustomerHighAmount_NewCustomerDigitalAboveThreshold_Hits()
    {
        var context = FraudContextMother.Default() with
        {
            IsNewCustomer = true,
            Delivery = DeliveryType.Digital,
            Amount = new Money(NewCustomerHighAmountRule.AmountThreshold + 1m, "BRL")
        };

        var result = new NewCustomerHighAmountRule().Evaluate(context);

        result.Hit.Should().BeTrue();
        result.RuleCode.Should().Be("NEW_CUSTOMER_HIGH_AMOUNT");
    }

    [Test]
    public void NewCustomerHighAmount_NewCustomerPhysicalAboveThreshold_DoesNotHit()
    {
        var context = FraudContextMother.Default() with
        {
            IsNewCustomer = true,
            Delivery = DeliveryType.Physical,
            Amount = new Money(NewCustomerHighAmountRule.AmountThreshold + 1m, "BRL")
        };

        var result = new NewCustomerHighAmountRule().Evaluate(context);

        result.Hit.Should().BeFalse();
    }

    [Test]
    public void NewCustomerHighAmount_ExistingCustomer_DoesNotHit()
    {
        var context = FraudContextMother.Default() with
        {
            IsNewCustomer = false,
            Amount = new Money(NewCustomerHighAmountRule.AmountThreshold + 1m, "BRL")
        };

        var result = new NewCustomerHighAmountRule().Evaluate(context);

        result.Hit.Should().BeFalse();
    }

    // --- BulkQuantityRule ---

    [Test]
    public void BulkQuantity_AtThreshold_Hits()
    {
        var context = FraudContextMother.Default() with { ItemCount = BulkQuantityRule.ItemCountThreshold };

        var result = new BulkQuantityRule().Evaluate(context);

        result.Hit.Should().BeTrue();
        result.RuleCode.Should().Be("BULK_QUANTITY");
    }

    [Test]
    public void BulkQuantity_BelowThreshold_DoesNotHit()
    {
        var context = FraudContextMother.Default() with { ItemCount = BulkQuantityRule.ItemCountThreshold - 1 };

        var result = new BulkQuantityRule().Evaluate(context);

        result.Hit.Should().BeFalse();
    }

    // --- CardVelocityRule ---

    [Test]
    public void CardVelocity_AtThreshold_Hits()
    {
        var context = FraudContextMother.Default() with
            { RecentTransactionsWithSameCard = CardVelocityRule.RecentTransactionThreshold };

        var result = new CardVelocityRule().Evaluate(context);

        result.Hit.Should().BeTrue();
        result.RuleCode.Should().Be("CARD_VELOCITY");
        result.Weight.Should().Be(CardVelocityRule.Weight);
    }

    [Test]
    public void CardVelocity_BelowThreshold_DoesNotHit()
    {
        var context = FraudContextMother.Default() with
            { RecentTransactionsWithSameCard = CardVelocityRule.RecentTransactionThreshold - 1 };

        var result = new CardVelocityRule().Evaluate(context);

        result.Hit.Should().BeFalse();
    }

    // --- Combined scenario S2: ebook caro + cliente novo → Rejected ---

    [Test]
    public void S2_HighValueDigitalAndNewCustomer_CombinedScoreExceedsRejectThreshold()
    {
        var context = FraudContextMother.Default() with
        {
            Amount = new Money(600m, "BRL"),
            Delivery = DeliveryType.Digital,
            IsNewCustomer = true
        };

        var set = new FraudRuleSet([new HighValueDigitalRule(), new NewCustomerHighAmountRule()]);
        var assessment = set.Evaluate(TxId, context, Now);

        assessment.Outcome.Should().Be(Outcome.Rejected);
    }

    // --- Combined scenario S3: bulk → Review ---

    [Test]
    public void S3_BulkQuantity_ResultsInReview()
    {
        var context = FraudContextMother.Default() with { ItemCount = 10 };

        var set = new FraudRuleSet([new BulkQuantityRule()]);
        var assessment = set.Evaluate(TxId, context, Now);

        assessment.Outcome.Should().Be(Outcome.Review);
    }
}
