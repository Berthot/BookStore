using Application.Commons;
using Application.UseCases.Sales.ApplyFraudDecision;
using Domain.Repositories;
using NSubstitute;
using Tests.Shared.Attributes;
using Tests.Shared.Base;
using Tests.Shared.Mothers.Sales;

namespace Tests.Application.UseCases.Sales.ApplyFraudDecision;

[Unit]
public sealed class ApplyFraudDecisionHandlerTests : UnitTestsBase
{
    private IPurchaseRepository _repo = null!;
    private IBookStoreUnitOfWork _uow = null!;
    private ApplyFraudDecisionHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<IPurchaseRepository>();
        _uow = Substitute.For<IBookStoreUnitOfWork>();
        _handler = new ApplyFraudDecisionHandler(_repo, _uow);
    }

    [Test]
    public async Task Handle_returns_not_found_when_no_purchase_for_transaction()
    {
        _repo.GetByTransactionIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Entities.Sales.Purchase?)null);

        var result = await _handler.Handle(new ApplyFraudDecisionRequest(Guid.NewGuid(), "Approved"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.NotFound);
    }

    [Test]
    public async Task Handle_approved_sets_status_to_confirmed()
    {
        var transactionId = Guid.NewGuid();
        var purchase = PurchaseMother.PendingFraudCheck();
        purchase.LinkTransaction(transactionId);
        _repo.GetByTransactionIdAsync(transactionId, Arg.Any<CancellationToken>()).Returns(purchase);

        var result = await _handler.Handle(new ApplyFraudDecisionRequest(transactionId, "Approved"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Status.Should().Be("CONFIRMED");
    }

    [Test]
    public async Task Handle_rejected_sets_status_to_cancelled()
    {
        var transactionId = Guid.NewGuid();
        var purchase = PurchaseMother.PendingFraudCheck();
        purchase.LinkTransaction(transactionId);
        _repo.GetByTransactionIdAsync(transactionId, Arg.Any<CancellationToken>()).Returns(purchase);

        var result = await _handler.Handle(new ApplyFraudDecisionRequest(transactionId, "Rejected"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Status.Should().Be("CANCELLED");
    }

    [Test]
    public async Task Handle_review_sets_status_to_under_review()
    {
        var transactionId = Guid.NewGuid();
        var purchase = PurchaseMother.PendingFraudCheck();
        purchase.LinkTransaction(transactionId);
        _repo.GetByTransactionIdAsync(transactionId, Arg.Any<CancellationToken>()).Returns(purchase);

        var result = await _handler.Handle(new ApplyFraudDecisionRequest(transactionId, "Review"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Status.Should().Be("UNDERREVIEW");
    }

    [Test]
    public async Task Handle_is_idempotent_when_purchase_already_confirmed()
    {
        var transactionId = Guid.NewGuid();
        var purchase = PurchaseMother.Confirmed();
        purchase.LinkTransaction(transactionId);
        _repo.GetByTransactionIdAsync(transactionId, Arg.Any<CancellationToken>()).Returns(purchase);

        var result = await _handler.Handle(new ApplyFraudDecisionRequest(transactionId, "Approved"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_under_review_can_be_confirmed_by_reviewer()
    {
        var transactionId = Guid.NewGuid();
        var purchase = PurchaseMother.UnderReview();
        purchase.LinkTransaction(transactionId);
        _repo.GetByTransactionIdAsync(transactionId, Arg.Any<CancellationToken>()).Returns(purchase);

        var result = await _handler.Handle(new ApplyFraudDecisionRequest(transactionId, "Approved"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Status.Should().Be("CONFIRMED");
    }

    [Test]
    public async Task Handle_commits_once_on_successful_decision()
    {
        var transactionId = Guid.NewGuid();
        var purchase = PurchaseMother.PendingFraudCheck();
        purchase.LinkTransaction(transactionId);
        _repo.GetByTransactionIdAsync(transactionId, Arg.Any<CancellationToken>()).Returns(purchase);

        await _handler.Handle(new ApplyFraudDecisionRequest(transactionId, "Approved"), CancellationToken.None);

        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }
}
