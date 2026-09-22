using Domain.Enums;
using Tests.Shared.Attributes;
using Tests.Shared.Mothers.Sales;

namespace Tests.Domain.Entities.Sales;

[Unit]
public sealed class PurchaseTests
{
    [Test]
    public void ApplyDecision_Approved_FromPendingFraudCheck_TransitionsToConfirmed()
    {
        var purchase = PurchaseMother.PendingFraudCheck();

        var error = purchase.ApplyDecision(Outcome.Approved);

        error.Should().BeNull();
        purchase.Status.Should().Be(PurchaseStatus.Confirmed);
    }

    [Test]
    public void ApplyDecision_Rejected_FromPendingFraudCheck_TransitionsToCancelled()
    {
        var purchase = PurchaseMother.PendingFraudCheck();

        var error = purchase.ApplyDecision(Outcome.Rejected);

        error.Should().BeNull();
        purchase.Status.Should().Be(PurchaseStatus.Cancelled);
    }

    [Test]
    public void ApplyDecision_Review_FromPendingFraudCheck_TransitionsToUnderReview()
    {
        var purchase = PurchaseMother.PendingFraudCheck();

        var error = purchase.ApplyDecision(Outcome.Review);

        error.Should().BeNull();
        purchase.Status.Should().Be(PurchaseStatus.UnderReview);
    }

    [Test]
    public void ApplyDecision_Approved_FromUnderReview_TransitionsToConfirmed()
    {
        var purchase = PurchaseMother.UnderReview();

        var error = purchase.ApplyDecision(Outcome.Approved);

        error.Should().BeNull();
        purchase.Status.Should().Be(PurchaseStatus.Confirmed);
    }

    [Test]
    public void ApplyDecision_Rejected_FromUnderReview_TransitionsToCancelled()
    {
        var purchase = PurchaseMother.UnderReview();

        var error = purchase.ApplyDecision(Outcome.Rejected);

        error.Should().BeNull();
        purchase.Status.Should().Be(PurchaseStatus.Cancelled);
    }

    [Test]
    public void ApplyDecision_Review_FromUnderReview_ReturnsDomainError()
    {
        var purchase = PurchaseMother.UnderReview();

        var error = purchase.ApplyDecision(Outcome.Review);

        error.Should().NotBeNull();
        error!.Code.Should().Be("PURCHASE_REVIEW_INVALID");
    }

    [Test]
    public void ApplyDecision_AnyOutcome_FromConfirmed_ReturnsDomainError()
    {
        var purchase = PurchaseMother.Confirmed();

        var error = purchase.ApplyDecision(Outcome.Rejected);

        error.Should().NotBeNull();
        error!.Code.Should().Be("PURCHASE_FINAL_STATE");
    }

    [Test]
    public void ApplyDecision_AnyOutcome_FromCancelled_ReturnsDomainError()
    {
        var purchase = PurchaseMother.Cancelled();

        var error = purchase.ApplyDecision(Outcome.Approved);

        error.Should().NotBeNull();
        error!.Code.Should().Be("PURCHASE_FINAL_STATE");
    }

    [Test]
    public void LinkTransaction_FirstTime_SetsTransactionId()
    {
        var purchase = PurchaseMother.PendingFraudCheck();
        var txId = Guid.NewGuid();

        var error = purchase.LinkTransaction(txId);

        error.Should().BeNull();
        purchase.TransactionId.Should().Be(txId);
    }

    [Test]
    public void LinkTransaction_SecondTime_ReturnsDomainError()
    {
        var purchase = PurchaseMother.PendingFraudCheck();
        purchase.LinkTransaction(Guid.NewGuid());

        var error = purchase.LinkTransaction(Guid.NewGuid());

        error.Should().NotBeNull();
        error!.Code.Should().Be("PURCHASE_TRANSACTION_ALREADY_LINKED");
    }
}
