using Application.Abstractions.Messaging;
using Application.Messages;
using Application.UseCases.Sales.ReconcilePendingPurchases;
using Domain.Entities.Sales;
using Domain.Repositories;
using NSubstitute;
using Tests.Shared.Attributes;
using Tests.Shared.Base;
using Tests.Shared.Constants;
using Tests.Shared.Mothers.Sales;

namespace Tests.Application.UseCases.Sales.ReconcilePendingPurchases;

[Unit]
public sealed class ReconcilePendingPurchasesHandlerTests : UnitTestsBase
{
    private IPurchaseRepository _repo = null!;
    private IEventPublisher _publisher = null!;
    private IBookStoreUnitOfWork _unitOfWork = null!;
    private ReconcilePendingPurchasesHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _repo = Substitute.For<IPurchaseRepository>();
        _publisher = Substitute.For<IEventPublisher>();
        _unitOfWork = Substitute.For<IBookStoreUnitOfWork>();
        _handler = new ReconcilePendingPurchasesHandler(_repo, _unitOfWork, _publisher);
    }

    [Test]
    public async Task Handle_republishes_stale_purchases()
    {
        var threshold = TestConstants.Dates.FixedUtcNow;
        var purchase = PurchaseMother.PendingFraudCheck();
        _repo.ListPendingFraudCheckAsync(threshold, Arg.Any<CancellationToken>())
            .Returns(new List<Purchase> { purchase });

        var result = await _handler.Handle(new ReconcilePendingPurchasesRequest(threshold), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Republished.Should().Be(1);
        await _publisher.Received(1).PublishAsync(
            Arg.Is<PurchasePlaced>(m => m.PurchaseId == purchase.Id && m.CorrelationId == purchase.CorrelationId),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_commits_after_publishing_so_outbox_messages_are_persisted()
    {
        var threshold = TestConstants.Dates.FixedUtcNow;
        _repo.ListPendingFraudCheckAsync(threshold, Arg.Any<CancellationToken>())
            .Returns(new List<Purchase> { PurchaseMother.PendingFraudCheck() });

        await _handler.Handle(new ReconcilePendingPurchasesRequest(threshold), CancellationToken.None);

        Received.InOrder(() =>
        {
            _publisher.PublishAsync(Arg.Any<PurchasePlaced>(), Arg.Any<CancellationToken>());
            _unitOfWork.CommitAsync(Arg.Any<CancellationToken>());
        });
    }

    [Test]
    public async Task Handle_does_not_republish_when_no_stale_purchases()
    {
        var threshold = TestConstants.Dates.FixedUtcNow;
        _repo.ListPendingFraudCheckAsync(threshold, Arg.Any<CancellationToken>())
            .Returns(new List<Purchase>());

        var result = await _handler.Handle(new ReconcilePendingPurchasesRequest(threshold), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Republished.Should().Be(0);
        await _publisher.DidNotReceive().PublishAsync(Arg.Any<PurchasePlaced>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_preserves_correlation_id_on_republish()
    {
        var threshold = TestConstants.Dates.FixedUtcNow;
        var purchase = new PurchaseBuilder().WithCorrelationId("original-corr-id").Build();
        _repo.ListPendingFraudCheckAsync(threshold, Arg.Any<CancellationToken>())
            .Returns(new List<Purchase> { purchase });

        await _handler.Handle(new ReconcilePendingPurchasesRequest(threshold), CancellationToken.None);

        await _publisher.Received(1).PublishAsync(
            Arg.Is<PurchasePlaced>(m => m.CorrelationId == "original-corr-id"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_republishes_each_stale_purchase_exactly_once()
    {
        var threshold = TestConstants.Dates.FixedUtcNow;
        var purchases = new List<Purchase>
        {
            new PurchaseBuilder().WithId(Guid.NewGuid()).WithCorrelationId("corr-1").Build(),
            new PurchaseBuilder().WithId(Guid.NewGuid()).WithCorrelationId("corr-2").Build()
        };
        _repo.ListPendingFraudCheckAsync(threshold, Arg.Any<CancellationToken>()).Returns(purchases);

        var result = await _handler.Handle(new ReconcilePendingPurchasesRequest(threshold), CancellationToken.None);

        result.Data!.Republished.Should().Be(2);
        await _publisher.Received(2).PublishAsync(Arg.Any<PurchasePlaced>(), Arg.Any<CancellationToken>());
    }
}
