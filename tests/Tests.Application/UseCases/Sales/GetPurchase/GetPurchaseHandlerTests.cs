using Application.Commons;
using Application.UseCases.Sales.GetPurchase;
using Domain.Enums;
using Domain.Repositories;
using NSubstitute;
using Tests.Shared.Attributes;
using Tests.Shared.Base;
using Tests.Shared.Mothers.Sales;

namespace Tests.Application.UseCases.Sales.GetPurchase;

[Unit]
public sealed class GetPurchaseHandlerTests : UnitTestsBase
{
    private IPurchaseRepository _repo = null!;
    private GetPurchaseHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<IPurchaseRepository>();
        _handler = new GetPurchaseHandler(_repo);
    }

    [Test]
    public async Task Handle_returns_not_found_when_purchase_does_not_exist()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Domain.Entities.Sales.Purchase?)null);

        var result = await _handler.Handle(new GetPurchaseRequest(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.NotFound);
    }

    [Test]
    public async Task Handle_customer_message_is_identical_for_pending_and_under_review()
    {
        var pending = PurchaseMother.PendingFraudCheck();
        var underReview = PurchaseMother.UnderReview();
        _repo.GetByIdAsync(pending.Id, Arg.Any<CancellationToken>()).Returns(pending);
        _repo.GetByIdAsync(underReview.Id, Arg.Any<CancellationToken>()).Returns(underReview);

        var pendingResult = await _handler.Handle(new GetPurchaseRequest(pending.Id), CancellationToken.None);
        var reviewResult = await _handler.Handle(new GetPurchaseRequest(underReview.Id), CancellationToken.None);

        pendingResult.Data!.CustomerMessage.Should().Be(reviewResult.Data!.CustomerMessage);
    }

    [Test]
    public async Task Handle_fraud_details_is_null_for_pending_status()
    {
        var pending = PurchaseMother.PendingFraudCheck();
        _repo.GetByIdAsync(pending.Id, Arg.Any<CancellationToken>()).Returns(pending);

        var pendingResult = await _handler.Handle(new GetPurchaseRequest(pending.Id), CancellationToken.None);

        pendingResult.Data!.FraudDetails.Should().BeNull();
    }

    [Test]
    public async Task Handle_fraud_details_exposes_review_outcome_for_under_review()
    {
        var underReview = PurchaseMother.UnderReview();
        _repo.GetByIdAsync(underReview.Id, Arg.Any<CancellationToken>()).Returns(underReview);

        var reviewResult = await _handler.Handle(new GetPurchaseRequest(underReview.Id), CancellationToken.None);

        reviewResult.Data!.FraudDetails.Should().NotBeNull();
        reviewResult.Data.FraudDetails!.Outcome.Should().Be(Outcome.Review);
    }

    [Test]
    public async Task Handle_returns_approved_outcome_for_confirmed_purchase_without_transaction()
    {
        var confirmed = PurchaseMother.Confirmed();
        _repo.GetByIdAsync(confirmed.Id, Arg.Any<CancellationToken>()).Returns(confirmed);

        var result = await _handler.Handle(new GetPurchaseRequest(confirmed.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.FraudDetails!.Outcome.Should().Be(Outcome.Approved);
    }

    [Test]
    public async Task Handle_returns_rejected_outcome_for_cancelled_purchase_without_transaction()
    {
        var cancelled = PurchaseMother.Cancelled();
        _repo.GetByIdAsync(cancelled.Id, Arg.Any<CancellationToken>()).Returns(cancelled);

        var result = await _handler.Handle(new GetPurchaseRequest(cancelled.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.FraudDetails!.Outcome.Should().Be(Outcome.Rejected);
    }

    [Test]
    public async Task Handle_returns_transaction_id_and_outcome_for_confirmed_purchase_with_transaction()
    {
        var transactionId = Guid.NewGuid();
        var purchase = new PurchaseBuilder().Build();
        purchase.LinkTransaction(transactionId);
        purchase.ApplyDecision(Outcome.Approved);

        _repo.GetByIdAsync(purchase.Id, Arg.Any<CancellationToken>()).Returns(purchase);

        var result = await _handler.Handle(new GetPurchaseRequest(purchase.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.FraudDetails!.TransactionId.Should().Be(transactionId);
        result.Data.FraudDetails.Outcome.Should().Be(Outcome.Approved);
        // No FraudOutcome snapshot stored when ApplyDecision is called without one
        result.Data.FraudDetails.Score.Should().BeNull();
        result.Data.FraudDetails.DecidedBy.Should().BeNull();
    }

    [Test]
    public async Task Handle_maps_status_to_uppercase()
    {
        var purchase = PurchaseMother.PendingFraudCheck();
        _repo.GetByIdAsync(purchase.Id, Arg.Any<CancellationToken>()).Returns(purchase);

        var result = await _handler.Handle(new GetPurchaseRequest(purchase.Id), CancellationToken.None);

        result.Data!.Status.Should().Be(PurchaseStatus.PendingFraudCheck);
    }
}
