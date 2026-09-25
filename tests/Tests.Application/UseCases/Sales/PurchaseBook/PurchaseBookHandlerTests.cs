using Application.Abstractions.Messaging;
using Application.Commons;
using Application.Messages;
using Application.UseCases.Sales.PurchaseBook;
using Domain.Enums;
using Domain.Repositories;
using NSubstitute;
using Tests.Shared.Attributes;
using Tests.Shared.Base;
using Tests.Shared.Mothers.Catalog;

namespace Tests.Application.UseCases.Sales.PurchaseBook;

[Unit]
public sealed class PurchaseBookHandlerTests : UnitTestsBase
{
    private IBookRepository _bookRepo = null!;
    private IPurchaseRepository _purchaseRepo = null!;
    private IBookStoreUnitOfWork _uow = null!;
    private IEventPublisher _publisher = null!;
    private PurchaseBookHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _bookRepo = Substitute.For<IBookRepository>();
        _purchaseRepo = Substitute.For<IPurchaseRepository>();
        _uow = Substitute.For<IBookStoreUnitOfWork>();
        _publisher = Substitute.For<IEventPublisher>();
        _handler = new PurchaseBookHandler(_bookRepo, _purchaseRepo, _uow, _publisher);
    }

    [Test]
    public async Task Handle_returns_not_found_when_book_does_not_exist()
    {
        _bookRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Domain.Entities.Catalog.Book?)null);

        var result = await _handler.Handle(ValidRequest(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ErrorCode.Should().Be(ErrorCode.NotFound);
    }

    [Test]
    public async Task Handle_creates_purchase_with_correct_total()
    {
        var book = new Tests.Shared.Mothers.Catalog.BookBuilder().WithPrice(30m).Build();
        _bookRepo.GetByIdAsync(book.Id, Arg.Any<CancellationToken>()).Returns(book);

        var result = await _handler.Handle(new PurchaseBookRequest(
            book.Id, 3, "CARD", "fp-001", "1234", "customer-1", "corr-1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _purchaseRepo.Received(1).Add(Arg.Is<Domain.Entities.Sales.Purchase>(p =>
            p.BookId == book.Id &&
            p.Quantity == 3 &&
            p.Total.Value == 90m));
    }

    [Test]
    public async Task Handle_publishes_purchase_placed_with_correct_data()
    {
        var book = BookMother.Ebook();
        _bookRepo.GetByIdAsync(book.Id, Arg.Any<CancellationToken>()).Returns(book);

        var result = await _handler.Handle(new PurchaseBookRequest(
            book.Id, 1, "CARD", "fp-xyz", "9999", "cust-99", "corr-99"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _publisher.Received(1).PublishAsync(
            Arg.Is<PurchasePlaced>(m =>
                m.BookId == book.Id &&
                m.Quantity == 1 &&
                m.CustomerId == "cust-99" &&
                m.PaymentFingerprint == "fp-xyz" &&
                m.BookFormat == BookFormat.Ebook.ToString()),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_commits_once_on_success()
    {
        var book = BookMother.Simple();
        _bookRepo.GetByIdAsync(book.Id, Arg.Any<CancellationToken>()).Returns(book);

        await _handler.Handle(new PurchaseBookRequest(
            book.Id, 1, "CARD", "fp-1", null, "cust-1", "corr-1"), CancellationToken.None);

        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_returns_pending_fraud_check_status()
    {
        var book = BookMother.Simple();
        _bookRepo.GetByIdAsync(book.Id, Arg.Any<CancellationToken>()).Returns(book);

        var result = await _handler.Handle(ValidRequest(book.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data!.Status.Should().Be(PurchaseStatus.PendingFraudCheck);
    }

    [Test]
    public async Task Handle_does_not_commit_when_book_not_found()
    {
        _bookRepo.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Domain.Entities.Catalog.Book?)null);

        await _handler.Handle(ValidRequest(), CancellationToken.None);

        await _uow.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Handle_publish_occurs_before_commit_for_outbox_atomicity()
    {
        var book = BookMother.Simple();
        _bookRepo.GetByIdAsync(book.Id, Arg.Any<CancellationToken>()).Returns(book);

        var order = new List<string>();
        _publisher.When(x => x.PublishAsync(Arg.Any<PurchasePlaced>(), Arg.Any<CancellationToken>()))
            .Do(_ => order.Add("publish"));
        _uow.When(x => x.CommitAsync(Arg.Any<CancellationToken>()))
            .Do(_ => order.Add("commit"));

        await _handler.Handle(ValidRequest(book.Id), CancellationToken.None);

        // PublishAsync stages the message in the EF outbox; CommitAsync flushes both together.
        order.Should().ContainInOrder("publish", "commit");
        order.Should().HaveCount(2);
    }

    private static PurchaseBookRequest ValidRequest(Guid? bookId = null) =>
        new(bookId ?? Guid.NewGuid(), 1, "CARD", "fp-default", "0000", "cust-default", "corr-default");
}
