using Application.UseCases.FraudAnalysis.SubmitTransaction;
using FluentValidation.TestHelper;
using Tests.Shared.Attributes;
using Tests.Shared.Base;

namespace Tests.Application.UseCases.FraudAnalysis.SubmitTransaction;

[Unit]
public sealed class SubmitTransactionValidatorTests : UnitTestsBase
{
    private readonly SubmitTransactionValidator _sut = new();

    private static SubmitTransactionRequest Valid() => new(
        ExternalReference: "REF-001",
        CustomerId: "customer-1",
        AmountValue: 100m,
        AmountCurrency: "BRL",
        PaymentType: "CREDIT",
        PaymentFingerprint: "fp-abc123",
        PaymentLast4: "1234",
        Channel: "WEB",
        Delivery: "DIGITAL",
        ItemCount: 1,
        OccurredAt: DateTime.UtcNow,
        CorrelationId: "corr-001");

    [Test]
    public void Valid_request_passes()
    {
        var result = _sut.TestValidate(Valid());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void CustomerId_empty_fails()
    {
        var result = _sut.TestValidate(Valid() with { CustomerId = "" });
        result.ShouldHaveValidationErrorFor(x => x.CustomerId);
    }

    [Test]
    public void AmountValue_zero_fails()
    {
        var result = _sut.TestValidate(Valid() with { AmountValue = 0m });
        result.ShouldHaveValidationErrorFor(x => x.AmountValue);
    }

    [Test]
    public void AmountValue_negative_fails()
    {
        var result = _sut.TestValidate(Valid() with { AmountValue = -1m });
        result.ShouldHaveValidationErrorFor(x => x.AmountValue);
    }

    [Test]
    public void AmountCurrency_empty_fails()
    {
        var result = _sut.TestValidate(Valid() with { AmountCurrency = "" });
        result.ShouldHaveValidationErrorFor(x => x.AmountCurrency);
    }

    [Test]
    public void PaymentFingerprint_empty_fails()
    {
        var result = _sut.TestValidate(Valid() with { PaymentFingerprint = "" });
        result.ShouldHaveValidationErrorFor(x => x.PaymentFingerprint);
    }

    [Test]
    public void ItemCount_zero_fails()
    {
        var result = _sut.TestValidate(Valid() with { ItemCount = 0 });
        result.ShouldHaveValidationErrorFor(x => x.ItemCount);
    }

    [Test]
    public void ItemCount_negative_fails()
    {
        var result = _sut.TestValidate(Valid() with { ItemCount = -1 });
        result.ShouldHaveValidationErrorFor(x => x.ItemCount);
    }

    [Test]
    public void CorrelationId_empty_fails()
    {
        var result = _sut.TestValidate(Valid() with { CorrelationId = "" });
        result.ShouldHaveValidationErrorFor(x => x.CorrelationId);
    }

    [Test]
    public void Channel_invalid_fails()
    {
        var result = _sut.TestValidate(Valid() with { Channel = "INVALID" });
        result.ShouldHaveValidationErrorFor(x => x.Channel);
    }

    [Test]
    [TestCase("WEB")]
    [TestCase("MOBILE")]
    [TestCase("API")]
    [TestCase("web")]
    public void Channel_valid_values_pass(string channel)
    {
        var result = _sut.TestValidate(Valid() with { Channel = channel });
        result.ShouldNotHaveValidationErrorFor(x => x.Channel);
    }

    [Test]
    public void Delivery_invalid_fails()
    {
        var result = _sut.TestValidate(Valid() with { Delivery = "EXPRESS" });
        result.ShouldHaveValidationErrorFor(x => x.Delivery);
    }

    [Test]
    [TestCase("DIGITAL")]
    [TestCase("PHYSICAL")]
    [TestCase("digital")]
    public void Delivery_valid_values_pass(string delivery)
    {
        var result = _sut.TestValidate(Valid() with { Delivery = delivery });
        result.ShouldNotHaveValidationErrorFor(x => x.Delivery);
    }
}
