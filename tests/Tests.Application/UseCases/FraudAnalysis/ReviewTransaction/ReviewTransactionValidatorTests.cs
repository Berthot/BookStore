using Application.UseCases.FraudAnalysis.ReviewTransaction;
using FluentValidation.TestHelper;
using Tests.Shared.Attributes;
using Tests.Shared.Base;

namespace Tests.Application.UseCases.FraudAnalysis.ReviewTransaction;

[Unit]
public sealed class ReviewTransactionValidatorTests : UnitTestsBase
{
    private readonly ReviewTransactionValidator _sut = new();

    private static ReviewTransactionRequest Valid() => new(
        TransactionId: Guid.NewGuid(),
        Outcome: "APPROVED",
        Justification: "No fraud signals detected.",
        ReviewerId: "reviewer-001");

    [Test]
    public void Valid_request_passes()
    {
        var result = _sut.TestValidate(Valid());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Test]
    public void Justification_empty_fails()
    {
        var result = _sut.TestValidate(Valid() with { Justification = "" });
        result.ShouldHaveValidationErrorFor(x => x.Justification);
    }

    [Test]
    public void Justification_whitespace_fails()
    {
        var result = _sut.TestValidate(Valid() with { Justification = "   " });
        result.ShouldHaveValidationErrorFor(x => x.Justification);
    }

    [Test]
    public void Outcome_null_fails()
    {
        var result = _sut.TestValidate(Valid() with { Outcome = null! });
        result.ShouldHaveValidationErrorFor(x => x.Outcome);
    }

    [Test]
    public void Outcome_invalid_fails()
    {
        var result = _sut.TestValidate(Valid() with { Outcome = "PENDING" });
        result.ShouldHaveValidationErrorFor(x => x.Outcome);
    }

    [Test]
    public void Outcome_review_fails()
    {
        var result = _sut.TestValidate(Valid() with { Outcome = "REVIEW" });
        result.ShouldHaveValidationErrorFor(x => x.Outcome);
    }

    [Test]
    [TestCase("APPROVED")]
    [TestCase("REJECTED")]
    [TestCase("approved")]
    [TestCase("rejected")]
    public void Outcome_valid_values_pass(string outcome)
    {
        var result = _sut.TestValidate(Valid() with { Outcome = outcome });
        result.ShouldNotHaveValidationErrorFor(x => x.Outcome);
    }
}
