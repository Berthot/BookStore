using Application.Commons;
using Application.UseCases.Sales.Ports;
using Application.UseCases.Sales.SubmitPurchaseToFraud;
using Domain.Repositories;
using NSubstitute;
using Tests.Shared.Attributes;
using Tests.Shared.Base;
using Tests.Shared.Constants;
using Tests.Shared.Mothers.Sales;

namespace Tests.Application.UseCases.Sales.SubmitPurchaseToFraud;

[Unit]
public sealed class SubmitPurchaseToFraudHandlerTests : UnitTestsBase
{
    private IPurchaseRepository _repo = null!;
    private IBookStoreUnitOfWork _uow = null!;
    private IFraudCheckGateway _gateway = null!;
    private SubmitPurchaseToFraudHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<IPurchaseRepository>();
        _uow = Substitute.For<IBookStoreUnitOfWork>();
        _gateway = Substitute.For<IFraudCheckGateway>();
        _handler = new SubmitPurchaseToFraudHandler(_repo, _uow, _gateway);
    }

    [Test]
    public async Task Handle_returns_not_found_when_purchase_missing()
    {
        _repo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Domain.Entities.Sales.Purchase?)null);

        var result = await _handler.Handle(ValidRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.NotFound);
    }

    [Test]
    public async Task Handle_is_idempotent_when_transaction_already_linked()
    {
        var existingTransactionId = Guid.NewGuid();
        var purchase = PurchaseMother.PendingFraudCheck();
        purchase.LinkTransaction(existingTransactionId);
        _repo.GetByIdAsync(purchase.Id, Arg.Any<CancellationToken>()).Returns(purchase);

        var result = await _handler.Handle(ValidRequest(purchase.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.TransactionId.Should().Be(existingTransactionId);
        await _gateway.DidNotReceive().SubmitAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<int>(),
            Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_calls_gateway_and_links_transaction()
    {
        var purchase = PurchaseMother.PendingFraudCheck();
        var newTransactionId = Guid.NewGuid();
        _repo.GetByIdAsync(purchase.Id, Arg.Any<CancellationToken>()).Returns(purchase);
        _gateway.SubmitAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<int>(),
            Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(newTransactionId);

        var result = await _handler.Handle(ValidRequest(purchase.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.TransactionId.Should().Be(newTransactionId);
        purchase.TransactionId.Should().Be(newTransactionId);
    }

    [Test]
    public async Task Handle_commits_once_after_linking()
    {
        var purchase = PurchaseMother.PendingFraudCheck();
        _repo.GetByIdAsync(purchase.Id, Arg.Any<CancellationToken>()).Returns(purchase);
        _gateway.SubmitAsync(
            Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<int>(),
            Arg.Any<decimal>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Guid.NewGuid());

        await _handler.Handle(ValidRequest(purchase.Id), CancellationToken.None);

        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_does_not_commit_when_already_idempotent()
    {
        var purchase = PurchaseMother.PendingFraudCheck();
        purchase.LinkTransaction(Guid.NewGuid());
        _repo.GetByIdAsync(purchase.Id, Arg.Any<CancellationToken>()).Returns(purchase);

        await _handler.Handle(ValidRequest(purchase.Id), CancellationToken.None);

        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    private static SubmitPurchaseToFraudRequest ValidRequest(Guid? purchaseId = null) =>
        new(
            purchaseId ?? TestConstants.Ids.PurchaseId,
            TestConstants.Ids.BookId,
            1,
            29.99m,
            "BRL",
            "EBOOK",
            "CARD",
            "fp-001",
            "1234",
            "cust-1",
            "corr-1",
            TestConstants.Dates.FixedUtcNow);
}
