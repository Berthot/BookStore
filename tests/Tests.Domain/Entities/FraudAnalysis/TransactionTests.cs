using Domain.Entities.FraudAnalysis;
using Domain.Enums;
using Domain.ValueObjects;
using Tests.Shared.Attributes;
using Tests.Shared.Constants;
using Tests.Shared.Mothers.FraudAnalysis;

namespace Tests.Domain.Entities.FraudAnalysis;

[Unit]
public sealed class TransactionTests
{
    [Test]
    [Description("I-01")]
    public void Create_WithZeroAmount_ReturnsDomainError()
    {
        var (tx, error) = new TransactionBuilder().WithAmount(0m).BuildCreate();

        tx.Should().BeNull();
        error.Should().NotBeNull();
        error!.Code.Should().Be("TRANSACTION_AMOUNT_INVALID");
    }

    [Test]
    [Description("I-01")]
    public void Create_WithNegativeAmount_ReturnsDomainError()
    {
        var (tx, error) = new TransactionBuilder().WithAmount(-1m).BuildCreate();

        tx.Should().BeNull();
        error!.Code.Should().Be("TRANSACTION_AMOUNT_INVALID");
    }

    [Test]
    [Description("I-01")]
    public void Create_WithPositiveAmount_Succeeds()
    {
        var (tx, error) = new TransactionBuilder().WithAmount(1m).BuildCreate();

        error.Should().BeNull();
        tx.Should().NotBeNull();
    }

    [Test]
    [Description("I-02")]
    public void StartProcessing_WhenAlreadyProcessing_ReturnsDomainError()
    {
        var tx = TransactionMother.Processing();

        var error = tx.StartProcessing();

        error.Should().NotBeNull();
        error!.Code.Should().Be("TRANSACTION_ALREADY_PROCESSING");
    }

    [Test]
    [Description("I-02")]
    public void StartProcessing_WhenDecided_ReturnsDomainError()
    {
        var tx = TransactionMother.DecidedApproved();

        var error = tx.StartProcessing();

        error.Should().NotBeNull();
        error!.Code.Should().Be("TRANSACTION_ALREADY_DECIDED");
    }

    [Test]
    [Description("I-03")]
    public void Decide_WhenNotProcessing_ReturnsDomainError()
    {
        var tx = TransactionMother.Received();

        var error = tx.Decide(AssessmentMother.EngineApproved(tx.Id));

        error.Should().NotBeNull();
        error!.Code.Should().Be("TRANSACTION_NOT_PROCESSING");
    }

    [Test]
    [Description("I-03")]
    public void Decide_WhenProcessing_AddsAssessmentAndTransitionsToDecided()
    {
        var tx = TransactionMother.Processing();

        var error = tx.Decide(AssessmentMother.EngineApproved(tx.Id));

        error.Should().BeNull();
        tx.Status.Should().Be(TransactionStatus.Decided);
        tx.Assessments.Should().HaveCount(1);
    }

    [Test]
    [Description("I-04")]
    public void Review_WhenStatusIsNotDecided_ReturnsDomainError()
    {
        var tx = TransactionMother.Processing();

        var error = tx.Review(AssessmentMother.ReviewerApproved(tx.Id));

        error.Should().NotBeNull();
        error!.Code.Should().Be("TRANSACTION_NOT_DECIDED");
    }

    [Test]
    [Description("I-04")]
    public void Review_WhenCurrentOutcomeIsNotReview_ReturnsDomainError()
    {
        var tx = TransactionMother.DecidedApproved();

        var error = tx.Review(AssessmentMother.ReviewerApproved(tx.Id));

        error.Should().NotBeNull();
        error!.Code.Should().Be("TRANSACTION_REVIEW_NOT_PENDING");
    }

    [Test]
    [Description("I-05")]
    public void Review_WithoutJustification_ReturnsDomainError()
    {
        var tx = TransactionMother.DecidedReview();
        var noJustification = Assessment.ForReviewer(
            tx.Id, Outcome.Approved, string.Empty, "reviewer-001",
            TestConstants.Dates.FixedUtcNow.AddMinutes(5));

        var error = tx.Review(noJustification);

        error.Should().NotBeNull();
        error!.Code.Should().Be("TRANSACTION_JUSTIFICATION_REQUIRED");
    }

    [Test]
    [Description("I-05")]
    public void Review_WithReviewOutcome_ReturnsDomainError()
    {
        var tx = TransactionMother.DecidedReview();
        var reviewOutcome = Assessment.ForReviewer(
            tx.Id, Outcome.Review, "Some justification", "reviewer-001",
            TestConstants.Dates.FixedUtcNow.AddMinutes(5));

        var error = tx.Review(reviewOutcome);

        error.Should().NotBeNull();
        error!.Code.Should().Be("TRANSACTION_REVIEW_OUTCOME_INVALID");
    }

    [Test]
    [Description("I-06")]
    public void Review_AfterReview_AppendsBothAssessments_PreviousRemains()
    {
        var tx = TransactionMother.DecidedReview();
        var initialCount = tx.Assessments.Count;

        tx.Review(AssessmentMother.ReviewerApproved(tx.Id));

        tx.Assessments.Should().HaveCount(initialCount + 1);
        tx.Assessments[0].Outcome.Should().Be(Outcome.Review);
    }

    [Test]
    [Description("I-07")]
    public void FailSafe_WhenNotProcessing_ReturnsDomainError()
    {
        var tx = TransactionMother.Received();

        var error = tx.FailSafe("system failure");

        error.Should().NotBeNull();
        error!.Code.Should().Be("TRANSACTION_NOT_PROCESSING");
    }

    [Test]
    [Description("I-07")]
    public void FailSafe_WhenProcessing_CreatesSystemAssessmentWithReviewOutcome()
    {
        var tx = TransactionMother.Processing();

        var error = tx.FailSafe("timeout during rule evaluation");

        error.Should().BeNull();
        tx.Status.Should().Be(TransactionStatus.Decided);
        var current = tx.CurrentAssessment();
        current.Should().NotBeNull();
        current!.Outcome.Should().Be(Outcome.Review);
        current.Decider.Kind.Should().Be(DeciderKind.System);
    }
}
